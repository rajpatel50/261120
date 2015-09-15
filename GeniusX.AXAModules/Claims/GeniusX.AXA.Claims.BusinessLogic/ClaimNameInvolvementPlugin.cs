using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common.Product;
using CoreBusinessLogic = Xiap.Claims.BusinessLogic;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Creates a SYSUPD Event when the main Claim Handler is changed, which updates the respective Claim Name Involvement. 
    /// The SYSUPD Event has a description of Claim Handler Change.
    /// The SYSUPD Event type is used when the system—rather than the user—generates an Event that is triggered by an action in the Business UI or by a K2 process. 
    /// For example, if the claim handler is changed. The description of the Event should indicate the reason that the SYSUPD Event was created.
    /// </summary>
    public class ClaimNameInvolvementPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Method used to call methods to update name involvement and add a claim detail on the basis of Claimant is Name Involvement on point component Change and Created.
        /// </summary>
        /// <param name="component">Component of Business Type</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">unique plugin id</param>
        /// <returns>collection of process results</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimNameInvolvement> pluginHelper = new PluginHelper<ClaimNameInvolvement>(point, (ClaimNameInvolvement)component, new ProcessResultsCollection());

            switch (point)
            {
                case ProcessInvocationPoint.ComponentChange:
                    this.GenerateEvent(pluginHelper);
                    break;

                case ProcessInvocationPoint.Created:
                    this.UpdateNameInvolvement(pluginHelper);
                    this.AddClaimDetailIfNIIsClaimaint(pluginHelper);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Adding a claim detail in claim if claimant exist in name involvement.
        /// </summary>
        /// <param name="pluginHelper">plugin helper of component type</param>
        private void AddClaimDetailIfNIIsClaimaint(PluginHelper<ClaimNameInvolvement> pluginHelper)
        {
            ClaimNameInvolvement claimNameInvolvement = pluginHelper.Component as ClaimNameInvolvement;
            ClaimHeader claimHeader = null;
            if (claimNameInvolvement.ClaimInvolvement != null)
            {
                claimHeader = claimNameInvolvement.ClaimInvolvement.ClaimHeader;
            }

            if (claimHeader != null)
            {
                ProductVersion productVersion = claimHeader.GetProduct();

                // If NI is Claimaint then Adding new ClaimDetail and Linking it with Claimaint NI. 
                if (productVersion != null && productVersion.Product != null && productVersion.Product.Code == ClaimConstants.PRODUCT_LIABCLAIM && claimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType.AdditionalClaimant && claimNameInvolvement.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
                {
                    ClaimDetail claimDetail = claimHeader.AddNewClaimDetail(ClaimConstants.CLAIM_DETAILTYPECODE_LIA); // adding a claim detail of type Liability.
                    var claimDetailToClaimInvolvementLinks = claimDetail.ClaimDetailToClaimInvolvementLinks.ToList();
                    // create a name involvement link added with claim detail.
                    if (claimDetailToClaimInvolvementLinks != null && CoreBusinessLogic.ClaimsBusinessLogicHelper.CheckProductClaimDetailToComponentLinkExist(claimDetail.GetProduct(), claimNameInvolvement.ClaimInvolvement.ProductLinkableComponentID.Value))
                    {
                        ClaimDetailToClaimInvolvementLink claimDetailLink = new ClaimDetailToClaimInvolvementLink(claimNameInvolvement.ClaimInvolvement) { ClaimDetail = claimDetail };
                        claimDetailToClaimInvolvementLinks.Add(claimDetailLink);
                    }
                }
            }
        }

        /// <summary>
        /// Update Name involvement when created.
        /// </summary>
        /// <param name="pluginHelper">plugin helper of component type</param>
        private void UpdateNameInvolvement(PluginHelper<ClaimNameInvolvement> pluginHelper)
        {
            ClaimNameInvolvement claimNameInvolvement = pluginHelper.Component as ClaimNameInvolvement;

            // If name involvement is Major Insured, set claimHeader.CustomReference01 = Client Reference, in Name Involvement of claim name involevement of CustomReference01
            if (claimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured)
            {
                ClaimHeader claimHeader = claimNameInvolvement.ClaimInvolvement.ClaimHeader;
                claimNameInvolvement.CustomReference01 = claimHeader.CustomReference01;   // UI Label = Reference
            }
        }

        /// <summary>
        /// Generate an event when component changes.
        /// </summary>
        /// <param name="pluginHelper">plugin helper of component type</param>
        private void GenerateEvent(PluginHelper<ClaimNameInvolvement> pluginHelper)
        {
            ClaimNameInvolvement claimNameInvolvement = pluginHelper.Component as ClaimNameInvolvement;
            string transactionType = claimNameInvolvement.Context.TransactionType;

            // If transaction type is of Create Claim.
            if (transactionType  != ClaimConstants.TRANSACTION_TYPE_CREATE_CLAIM)
            {
                if (claimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType.MainClaimHandler)
                {
                    if (claimNameInvolvement.DirtyPropertyList.ContainsKey(ClaimConstants.Name_ID))
                    {
                        if (claimNameInvolvement.DirtyPropertyList.Where(x => x.Key == ClaimConstants.Name_ID).FirstOrDefault().Value.OriginalValue != null)
                        {
                            this.AddChangeHandlerEvent(claimNameInvolvement);
                        }
                    }
                    else if (transactionType == ClaimConstants.TRANSACTION_TYPE_CHANGE_CLAIM_HANDLER // if transaction type is ChangeClaimHandler.
                        && claimNameInvolvement.DirtyPropertyList.ContainsKey(ClaimConstants.NAME_INVOLVEMENT_MAINTENANCE_STATUS)
                        && claimNameInvolvement.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest
                        && claimNameInvolvement.DirtyPropertyList[ClaimConstants.NAME_INVOLVEMENT_MAINTENANCE_STATUS].OriginalValue != null)
                    {
                        this.AddChangeHandlerEvent(claimNameInvolvement);
                    }
                }
            }
        }

        /// <summary>
        /// Adding a change Handler event.
        /// </summary>
        /// <param name="claimNameInvolvement">Claim Name Involvement</param>
        private void AddChangeHandlerEvent(ClaimNameInvolvement claimNameInvolvement)
        {
            ClaimHeader claimHeader = claimNameInvolvement.Parent.Parent as ClaimHeader;
            var productEvents = ProductService.GetProductEventQuery().GetProductEvents(claimHeader.ProductVersionID.GetValueOrDefault());
            if (productEvents.Any(x => x.EventTypeCode == ClaimConstants.SYSUPD_Event_Type))
            {
                var productEvent = productEvents.Where(x => x.EventTypeCode == ClaimConstants.SYSUPD_Event_Type).First();
                ClaimEvent claimEvent = claimHeader.AddNewClaimEvent(productEvent.ProductEventID, true);
                claimEvent.EventDescription = "Claim Handler changed";
            }
        }
    }
}
