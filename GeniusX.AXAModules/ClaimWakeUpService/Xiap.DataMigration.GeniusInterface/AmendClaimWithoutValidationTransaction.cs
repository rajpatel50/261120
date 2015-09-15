using System;
using System.Configuration;
using System.Linq;
using System.Transactions;

using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessTransaction;
using Xiap.Framework;
using Xiap.Framework.Caching;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace Xiap.DataMigration.GeniusInterface.AXACS
{
    [Serializable]
    public class AmendClaimWithoutValidationTransaction : AbstractClaimsBusinessTransaction
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
        private const string MinDurationToLogConfigSetting = "TransactionMethodsMinimumDurationToPerfLog";
		
        private static readonly int MinDurationToLog = (string.IsNullOrEmpty(ConfigurationManager.AppSettings[MinDurationToLogConfigSetting]) ? -1 : int.Parse(ConfigurationManager.AppSettings[MinDurationToLogConfigSetting]));

        #region Constructor
        public AmendClaimWithoutValidationTransaction()
            : base(ClaimsEntitiesFactory.GetClaimsEntities(), "Claims", ClaimsProcessConstants.AMENDCLAIM, null)
        {
        }
        #endregion

        #region Overridden Properties
        public override string Description
        {
            get { return "Amends a Claim"; }
        }

        public override string SecurityToken
        {
            get { return ClaimsSecurityConstant.CLAIM_BT_AMENDCLAIM; }
        }

        public override string ComponentSecurityToken
        {
            get { return ClaimsSecurityConstant.CLAIM_SC_CLAIMHEADERUPDATE; }
        }
        #endregion

        public new void Complete(bool treatWarningsAsErrors)
        {

            using (var logger = new PerfLogger(this.GetType(), "Complete", MinDurationToLog))
            {
                //if (treatWarningsAsErrors)
                //{
                //    this.SeverityLimit = ErrorSeverity.Information;
                //}
                //else
                //{
                //    this.SeverityLimit = ErrorSeverity.Warning;
                //}

                // Validate before performing any transactional operations
                //this.Validate(ValidationMode.Full);
                this.CheckResults(ProcessInvocationPoint.Validation, ErrorSeverity.Fatal);
                this.CheckResults(TransactionInvocationPoint.PreValidate, ErrorSeverity.Fatal);
                this.CheckResults(TransactionInvocationPoint.PostValidate, ErrorSeverity.Fatal);

                // begin transaction to update data
                using (var ts = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = TransactionIsolationLevel }))
                {

                    //var prop = typeof(AbstractBusinessTransaction).GetProperty("ProcessHandler");
                    //var processHandler = (IBusinessProcessHandler)prop.GetValue(this, null);
                    //base.ProcessHandler.PreComplete(this);
                    //this.CheckResults(TransactionInvocationPoint.PreComplete, this.SeverityLimit);

                    this.DoSave();
                    this.CheckResults(TransactionInvocationPoint.Save, this.SeverityLimit);

                    this.DoComplete();
                    this.CheckResults(TransactionInvocationPoint.Complete, this.SeverityLimit);

                    // Code running in PostComplete must not attempt to update any data in the object context
                    //this.ProcessHandler.PostComplete(this);

                    this.CheckResults(ErrorSeverity.Fatal);

                    if (_Logger.IsWarnEnabled && Transaction.Current != null)
                    {
                        Guid identifier = Transaction.Current.TransactionInformation.DistributedIdentifier;
                        if (identifier != Guid.Empty)
                        {
                            _Logger.Warn("FlattenedTransaction promoted to DTC with DistributedIdentifier " + identifier);
                        }
                    }

                    // Commits the changes
                    ts.Complete();
                    //this._isActive = false;
                    //this._isExecuted = false;
                }

                // FlattenedTransaction has been committed so even if an error occurs the transaction is done with and needs to be removed
                try
                {
                    this.DoPostCommit();
                    this.CheckResults(TransactionInvocationPoint.PostCommit, ErrorSeverity.Fatal);
                }
                finally
                {
                    // Remove the business transaction object from cache.
                    // which has been added from BusinessTransactionFactory 
                    XiapTransactionEvents.RaiseOnTransactionEnd(this.Context.TransactionId);
                }
            }
        }

        #region Overridden Methods
        protected override void DoStart(string[] parameters)
        {
            if (parameters == null || parameters.Length < 1)
            {
                this.InvokeErrorMessage(MessageConstants.CLAIM_INVALID_NO_OF_PARAMETERS);
            }

            var claimHeaderId = Convert.ToInt64(parameters[0]);
            if (claimHeaderId == 0)
            {
                this.InvokeErrorMessage("Claim Header ID is not present.");
            }

            if (parameters.Length >= 2)
            {
                this.Context.IsSystemProcess = Convert.ToBoolean(parameters[1]);
            }

            //if (parameters.Length >= 3)
            //{
            //    string virtualTransactionId = parameters[2];
            //    if (!string.IsNullOrEmpty(virtualTransactionId))
            //    {
            //        this.ClaimsContext.VirtualTransactionId = virtualTransactionId;
            //    }
            //}

            if (parameters.Length >= 4)
            {
                this.IsAmendingNotes = Convert.ToBoolean(parameters[3]);

                // This is to ensure that child components are not validated
                if (this.IsAmendingNotes)
                {
                    this.ValidationMode = ValidationMode.Loaded;
                }
            }

            //this.ValidateAmend(claimReference, (short)StaticValues.ClaimStage.Claim, this.IsAmendingNotes);
            this.InitializeAmend(claimHeaderId, BusinessProcessUsage.Updateable, StaticValues.ClaimStage.Claim, MessageConstants.CLAIM_INVALID_CLAIMREFERENCE);
        }

        protected override void DoComponentLoad()
        {
            if (this.Component == null)
            {
                this.Component = this.GetClaimComponent(this.Parameters[0].ToString(), MessageConstants.CLAIM_INVALID_CLAIMREFERENCE);
            }
        }

        protected override void DoComplete()
        {
            this.UpdateClaimPropertiesWhenSaved();
            base.DoComplete();
        }
        #endregion

        protected void InitializeAmend(long claimHeaderId, BusinessProcessUsage usage, StaticValues.ClaimStage claimStage, string messageAlias)
        {
            this.Initialize(claimHeaderId, usage, claimStage, messageAlias);
        }

        protected void Initialize(long claimHeaderId, BusinessProcessUsage usage, StaticValues.ClaimStage claimStage, string messageAlias)
        {
            if (this.Component == null)
            {
                this.Component = this.GetClaimComponent(claimHeaderId, messageAlias);
            }

            if (!this.CheckValidClaimsProduct(this.Component as ClaimHeader, (short)claimStage))
            {
                this.InvokeErrorMessage(MessageConstants.INVALID_PRODUCT_CODE);
            }

            this.UpdateDataContext(ClaimHeader.ClaimProductVersionID.Value, usage);
        }

        protected ClaimHeader GetClaimComponent(long claimHeaderId, string messageAlias)
        {
            if (claimHeaderId != 0)
            {
                var entities = (ClaimsEntities)ObjectContext;
                ClaimHeader claimHeader = (from row in entities.ClaimHeader
                                           where row.ClaimHeaderID == claimHeaderId
                                           select row).FirstOrDefault();

                if (claimHeader == null)
                {
                    string messageBody;
                    string messageTitle;

                    MessageServiceFactory.GetMessageService().GetMessage(messageAlias, out messageTitle, out messageBody, claimHeaderId);
                    throw new ArgumentException(messageBody);
                }

                return claimHeader;
            }

            return null;
        }
    }
}
