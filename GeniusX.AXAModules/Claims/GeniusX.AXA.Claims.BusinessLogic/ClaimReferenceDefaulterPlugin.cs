using System;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.Locking;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework.Security;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Claim Reference Defaulter Plugin
    /// </summary>
    public class ClaimReferenceDefaulterPlugin : AbstractComponentPlugin
    {
        // adding a logger for creating info in log file.
        private readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Setting values when component create, copy and when component changes.
        /// </summary>
        /// <param name="component">Component of Business Type</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">unique plugin id</param>
        /// <returns>collection of process results</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            ProcessResultsCollection processResults = new ProcessResultsCollection();

            if (point == ProcessInvocationPoint.Created ||  point == ProcessInvocationPoint.Copy || point == ProcessInvocationPoint.ComponentChange)
            {
                try
                {
                    bool IsClaimHeaderDefaulted = false;
                    ClaimHeader claimHeader = (ClaimHeader)component;

                    // if Claim Reference field property gets changes.
                    if (point == ProcessInvocationPoint.ComponentChange && !claimHeader.PropertiesChanged.ContainsKey(ClaimHeader.ClaimReferenceFieldName))
                    {
                        return processResults;
                    }

                    Product product = claimHeader.GetProduct().GetProduct();
                    string createdBy = claimHeader.CustomReference05;   // UI Label = Created By

                    if (point == ProcessInvocationPoint.Copy)
                    {
                        claimHeader = component.Context.CopyDictionary[component.DataId] as ClaimHeader;
                        claimHeader.CustomReference05 = createdBy; // claimHeader.CustomReference05 = Created By
                        product = claimHeader.GetProduct().GetProduct();
                    }

                    if (claimHeader != null)
                    {
                        if (!claimHeader.ClaimStage.HasValue)
                        {
                            claimHeader.ClaimStage = product.ClaimStage;
                        }

                        if (claimHeader.ClaimStage.GetValueOrDefault((short)StaticValues.ClaimStage.Claim) != (short)StaticValues.ClaimStage.Claim)
                        {
                            return processResults;
                        }

                        bool isUniqueClaimRef = false;
                        bool isLockSuccess = false;

                        for (int loopCounter = 0; loopCounter < 5; loopCounter++)
                        {
                            isUniqueClaimRef = false;
                            isLockSuccess = false;

                            if (string.IsNullOrEmpty(claimHeader.ClaimReference))
                            {
                                claimHeader.ClaimReference = this.AllocateClaimReference(claimHeader, product, ref IsClaimHeaderDefaulted);
                            }

                            isUniqueClaimRef = this.IsUniqueClaimReference(claimHeader, point, IsClaimHeaderDefaulted);

                            isLockSuccess = IsCreateLockStatus(claimHeader);

                            if (isUniqueClaimRef && isLockSuccess)
                            {
                                break;
                            }

                            if (!IsClaimHeaderDefaulted || isLockSuccess)
                            {
                                break;
                            }

                            claimHeader.ClaimReference = string.Empty;
                        }

                        // if Claim reference is not unique then add error.
                        if (!isUniqueClaimRef)
                        {
                            ClaimsBusinessLogicHelper.AddError(processResults, MessageConstants.CLAIM_REFERENCE_EXIST, point, component);
                        }

                        if (isUniqueClaimRef && !isLockSuccess)
                        {
                            ClaimsBusinessLogicHelper.AddError(processResults, MessageConstants.ERROR_IN_LOCKING, point, claimHeader);
                        }
                    }
                }
                catch (Exception e)
                {
                    this._Logger.Error(e);
                    this._Logger.Info(e.StackTrace);
                }
            }

            return processResults;
        }

        /// <summary>
        /// Updating the locks on claim header.
        /// </summary>
        /// <param name="claimHeader">component of type Claim header</param>
        /// <returns>bool value</returns>
        private static bool IsCreateLockStatus(ClaimHeader claimHeader)
        {
            if (!String.IsNullOrEmpty(claimHeader.ClaimReference))
            {
                return LockManager.UpdateReferenceLocks(claimHeader.Context.TransactionId, claimHeader.ClaimReference, LockLevel.ClaimReference, LockType.Update, LockDurationType.Transaction, LockOrigin.ClaimInput, string.Empty);
            }

            return false;
        }

        /// <summary>
        /// Generating a claim reference and allocate it on claim haeder.
        /// </summary>
        /// <param name="claimHeader">component of type claim header</param>
        /// <param name="product">product type</param>
        /// <param name="isClaimHeaderDefaulted">bool value</param>
        /// <returns>claim reference</returns>
        private string AllocateClaimReference(ClaimHeader claimHeader, Product product, ref bool isClaimHeaderDefaulted)
        {
            if (product != null)
            {
                this.GenerateClaimReference(product, claimHeader);
                isClaimHeaderDefaulted = true;
            }

            return claimHeader.ClaimReference;
        }

        /// <summary>
        /// Checking whether the generated claim reference is unique or not.
        /// </summary>
        /// <param name="claimHeader">component of type claim header</param>
        /// <param name="point">Invocation Point</param>
        /// <param name="isClaimHeaderDefaulted">bool value</param>
        /// <returns>bool value</returns>
        private bool IsUniqueClaimReference(ClaimHeader claimHeader, ProcessInvocationPoint point, bool isClaimHeaderDefaulted)
        {
            this._Logger.Debug("UniqueClaimReference Check");
            bool isUniqueClaimRef = false;
            if (!String.IsNullOrEmpty(claimHeader.ClaimReference) && (claimHeader.DirtyPropertyList.ContainsKey("ClaimReference") || isClaimHeaderDefaulted || point == ProcessInvocationPoint.Validation))
            {
                ClaimsEntities entities = ClaimsEntitiesFactory.GetClaimsEntities();
                var rowCount = (from row in entities.ClaimHeader where row.ClaimReference.Equals(claimHeader.ClaimReference) && row.ClaimHeaderID != claimHeader.ClaimHeaderID select row).Count();
                if (rowCount == 0)
                {
                    isUniqueClaimRef = true;
                }
            }

            return isUniqueClaimRef;
        }

        /// <summary>
        /// Method used for generating claim refernce
        /// </summary>
        /// <param name="product">type product</param>
        /// <param name="claimHeader">claim header</param>
        private void GenerateClaimReference(Product product, ClaimHeader claimHeader)
        {
            string prefixString = this.GetReferencePrefix();
            if (this._Logger.IsDebugEnabled)
            {
                this._Logger.Debug("Prefix String: " + prefixString);
            }

            string sequence = LockManager.AllocateReference(prefixString, ReferenceType.ClaimReference, string.Empty, "0000001", 7, "9999999", false);
            if (this._Logger.IsDebugEnabled)
            {
                this._Logger.Debug("sequence: " + sequence);
            }

            string suffixString = this.GenerateReferenceSuffix(claimHeader.ClaimHeaderAnalysisCode01);
            claimHeader.ClaimReference = string.Format("{0}{1}{2}", prefixString, sequence, suffixString);
            if (this._Logger.IsDebugEnabled)
            {
                this._Logger.Debug("Generated Claim Reference : " + claimHeader.ClaimReference);
            }
        }

        /// <summary>
        /// generating prefix to add in reference.
        /// </summary>
        /// <returns>string value</returns>
        private string GetReferencePrefix()
        {
            IDecisionTableComponent decisionTableComponent = null;
            IDecisionTableHelper _decisiontablehelper = ObjectFactory.Resolve<IDecisionTableHelper>();
            string customCode01 =  XiapSecurity.GetUser().CustomCode01;   // UI Label = Dept; User
            if (this._Logger.IsDebugEnabled)
            {
                this._Logger.Debug("CustomCode01:" + customCode01);   // UI Label = Dept
            }

            decisionTableComponent = _decisiontablehelper.Call(ClaimConstants.CLAIMREFERENCEPREFIX_DECISIONTABLE, DateTime.Today, customCode01);   // UI Label = Dept; User

            //// decisionTableComponent.Action1 = Department Code
            if (decisionTableComponent.Action1 != null && !string.IsNullOrWhiteSpace(decisionTableComponent.Action1.ToString())) 
            {
                if (this._Logger.IsDebugEnabled)
                {
                    this._Logger.Debug("decisionTableComponent.Action1: " + decisionTableComponent.Action1);
                }

                return decisionTableComponent.Action1.ToString().ToUpper();
            }
            
            return string.Empty;
        }

        /// <summary>
        /// generate suffix value for claim reference
        /// </summary>
        /// <param name="claimHeaderAnalysisCode01">claim header analysis code</param>
        /// <returns>string value</returns>
        private string GenerateReferenceSuffix(string claimHeaderAnalysisCode01)
        {
            if (this._Logger.IsDebugEnabled)
            {
                this._Logger.Debug("claimHeaderAnalysisCode01: " + claimHeaderAnalysisCode01);
            }

            if (string.IsNullOrEmpty(claimHeaderAnalysisCode01))
            {
                return string.Empty;
            }

            if (claimHeaderAnalysisCode01.Length < 2)
            {
                return claimHeaderAnalysisCode01;
            }

            return claimHeaderAnalysisCode01.Substring(0, 2);
        }	
    }
}
