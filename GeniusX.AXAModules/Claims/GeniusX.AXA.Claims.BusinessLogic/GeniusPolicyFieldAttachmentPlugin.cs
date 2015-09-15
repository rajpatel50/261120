using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Sets the DateofEventFrom field to the value of DateofLossFrom.
    /// <para/>
    /// When the PolicyHeaderID is changed, that is, when the value is set on the Policy attachment process, sets:
    /// - The AccountingCurrencyCode on the Claim Header from the Terms on the Policy.
    /// - The MajorInsured, MajorClaimant and LossBroker Names on the Claim Name Involvements to the corresponding Names taken from the Policy.
    /// <para/>
    /// For an Edit Claim transaction where the Claim is attached to a Policy, it makes the Date of Loss and Date of Loss Type fields read-only.
    /// </summary>  
    public class GeniusPolicyFieldAttachmentPlugin : AbstractComponentPlugin
    {
        /// <summary>
        ///  Invocation Method of plugin 
        /// </summary>
        /// <param name="component">Component name </param>
        /// <param name="point"> plugin Invocation point  </param>
        /// <param name="pluginId">Plugin id</param>
        /// <returns>Result Collection</returns>
        public override ProcessResultsCollection ProcessComponent(Xiap.Framework.IBusinessComponent component, Xiap.Framework.ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimHeader> pluginHelper = new PluginHelper<ClaimHeader>(point, (ClaimHeader)component, new ProcessResultsCollection());
            switch (point)
            {
                case Xiap.Framework.ProcessInvocationPoint.Created:
                    this.SetDateOfEventFromDateOfLoss(pluginHelper);
                    break;

                case Xiap.Framework.ProcessInvocationPoint.ComponentChange:
                    this.AssignGeniusPolicyFields(pluginHelper);
                    this.ComponentChangeDefaulting(pluginHelper);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Retrive field for ClaimHeader Component  
        /// </summary>
        /// <param name="component">Claim Header </param>
        /// <param name="point"> Invocation point -FieldRetrieval</param>
        /// <param name="field">Field Name </param>
        /// <param name="pluginId">Plug In ID</param>
        /// <returns>Result Collection</returns>
        public override ProcessResultsCollection FieldRetrieval(IBusinessComponent component, ProcessInvocationPoint point, ref Xiap.Framework.Metadata.Field field, int pluginId)
        {
            ClaimHeader claimHeader = component as ClaimHeader;
            if ((field.PropertyName == ClaimHeader.DateOfLossTypeCodeFieldName || field.PropertyName == ClaimHeader.DateOfLossFromFieldName) && claimHeader.Context.TransactionType == ClaimConstants.TRANSACTION_TYPE_AMEND_CLAIM && claimHeader.PolicyHeaderID != null && claimHeader.PropertiesChanged.ContainsKey(ClaimConstants.POLICY_HEADER_ID) == false)
            {
                field.Readonly = true;
            }
            
            return null;
        }

        /// <summary>
        /// Set Date Of Event  from Date of loss 
        /// </summary>
        /// <param name="pluginHelper">Pass the obeject of plugin helper which contains Component</param>
        private void SetDateOfEventFromDateOfLoss(PluginHelper<ClaimHeader> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component as ClaimHeader;
            claimHeader.DateOfEventFrom = claimHeader.DateOfLossFrom;
        }

        /// <summary>
        /// Assign genius field to Genisu.X component 
        /// </summary>
        /// <param name="pluginHelper">Pass the obeject of plugin helper which contains Component</param>
        private void AssignGeniusPolicyFields(PluginHelper<ClaimHeader> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component as ClaimHeader;
            if (claimHeader.PropertiesChanged != null && claimHeader.PropertiesChanged.ContainsKey(ClaimConstants.POLICY_HEADER_ID))
            {
                if (claimHeader.UWHeader != null)
                {
                    if (claimHeader.UWHeader.ITerms != null && claimHeader.UWHeader.ITerms.Count > 0)
                    {
                        IUWTerms uwTerm = claimHeader.UWHeader.ITerms.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
                        if (string.IsNullOrWhiteSpace(uwTerm.MainAccountingCurrencyCode) == false)
                        {
                            claimHeader.AccountingCurrencyCode = uwTerm.MainAccountingCurrencyCode;
                        }
                        else
                        {
                            claimHeader.AccountingCurrencyCode = ClaimConstants.DEFAULT_CURRENCY_CODE;
                        }
                    }

                    long? nameID = null;
                    if (claimHeader.UWHeader.IUWNameInvolvements != null)
                    {
                        var uwNI = claimHeader.UWHeader.IUWNameInvolvements.Where(x => x.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured).FirstOrDefault();
                        if (uwNI != null)
                        {
                            nameID = uwNI.NameID;
                            this.SetNameInvolvementNameID(claimHeader, (short)StaticValues.NameInvolvementType.MajorInsured, nameID);
                            this.SetNameInvolvementNameID(claimHeader, (short)StaticValues.NameInvolvementType.MajorClaimant, nameID);
                        }
                    }

                    if (claimHeader.UWHeader.IUWNameInvolvements != null)
                    {
                        var uwNI = claimHeader.UWHeader.IUWNameInvolvements.Where(x => x.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorBroker).FirstOrDefault();
                        if (uwNI != null)
                        {
                            // Updating the Loss Broker NI
                            this.SetNameInvolvementNameID(claimHeader, (short)StaticValues.NameInvolvementType.LossBroker, uwNI.NameID);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// On cahnge of Component Set Date Of Loss From Value 
        /// </summary>
        /// <param name="pluginHelper">Pass the obeject of plugin helper which contains Component</param>
        private void ComponentChangeDefaulting(PluginHelper<ClaimHeader> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component as ClaimHeader;
            if (claimHeader.PolicyHeaderID == null)
            {
                if (claimHeader.DateOfLossTypeCode == ClaimConstants.DATE_OF_LOSS_TYPE_CODE_OCCURRENCE && claimHeader.PropertiesChanged.ContainsKey(ClaimConstants.DATE_OF_EVENT_FROM))
                {
                    claimHeader.DateOfLossFrom = claimHeader.DateOfEventFrom;
                }
            }
        }

        /// <summary>
        /// Set Name Involvement NameID on basis of Name Involvement Type and Name Involvement Maintenance Status
        /// </summary>
        /// <param name="claimHeader">claim Header</param>
        /// <param name="nameInvolvementType"> type of name  Involvement </param>
        /// <param name="nameID"> Involvement ID</param>
        private void SetNameInvolvementNameID(ClaimHeader claimHeader, short nameInvolvementType, long? nameID)
        {
            ClaimInvolvement claimInvolvement = null;
            ClaimNameInvolvement nameInvolvement = null;

            if (claimHeader.ClaimInvolvements != null)
            {
                if (claimHeader.ClaimInvolvements.Any(x => x.ClaimNameInvolvements.Any(y => y.NameInvolvementType == nameInvolvementType && y.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)))
                {
                    claimInvolvement = claimHeader.ClaimInvolvements.Where(x => x.ClaimNameInvolvements.Where(y => y.NameInvolvementType == nameInvolvementType && y.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).Count() > 0).FirstOrDefault();
                    nameInvolvement = claimInvolvement.ClaimNameInvolvements.Where(x => x.NameInvolvementType == nameInvolvementType && x.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();
                    nameInvolvement.NameID = nameID;
                }
            }
        }
    }
}
