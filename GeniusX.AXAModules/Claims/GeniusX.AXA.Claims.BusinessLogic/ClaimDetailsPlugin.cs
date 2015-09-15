using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common.Product;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ClaimDetailsPlugin : AbstractComponentPlugin
    {
        private const string EstimateSettledEventTypeCode = "EstimateSettledEventTypeCode";
        private const string EstimateReviewedEventTypeCode = "EstimateReviewedEventTypeCode";

        /// <summary>
        /// Call on ClaimDetail Component
        /// </summary>
        /// <param name="component">Claim Detail</param>
        /// <param name="point">Points Create,Virtual,ComponentChange,PreCreateValidation</param>
        /// <param name="pluginId">PlugIn ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimDetail> pluginHelper = new PluginHelper<ClaimDetail>(point, component as ClaimDetail, new ProcessResultsCollection());

            switch (point)
            {
                // Set the claim title and check whether we default the isFunded? and Excess values from the Claim Header
                case ProcessInvocationPoint.Created:
                    this.SetClaimDetailTitle(pluginHelper.Component as ClaimDetail, point);
					this.InitializeExcess(pluginHelper.Component);
                    break;

                // Raise a claim inactivity event on the deatail if it's been closed; if this CD is attached at coverage level
                // attach all other unattached coverages to the same Policy Coverage
                case ProcessInvocationPoint.ComponentChange:
                    this.ClaimDetailStatusChangeEvent(pluginHelper);
                    this.AttacheAllClaimDetailsToCoverage(pluginHelper);
                    break;

                // Add estimate reviewed event, if applicable.
                case ProcessInvocationPoint.Virtual:
                    ClaimDetail cd = pluginHelper.Component as ClaimDetail;
                    this.CreateReviewEvent(cd);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Attach all other claimdetails to this claim detail's coverage
        /// </summary>
        /// <param name="pluginHelper">ClaimDetail type PlugHelper</param>
        private void AttacheAllClaimDetailsToCoverage(PluginHelper<ClaimDetail> pluginHelper)
        {
            ClaimDetail claimDetail = pluginHelper.Component;
            // Check this Claim Detail has had the Policy Coverage link changed to another policy coverage.
            if (claimDetail.PolicyCoverageID != null && claimDetail.PolicyLinkLevel != null && claimDetail.PropertiesChanged.ContainsKey(ClaimConstants.POLICY_COVERAGE_ID) && claimDetail.PropertiesChanged.ContainsKey(ClaimConstants.POLICY_LINK_LEVEL))
            {
                ClaimHeader claimHeader = claimDetail.Parent as ClaimHeader;
                // if so, find all other ClaimDetails not attached to a policy component
                var allUnAttachedClaimDetails = claimHeader.ClaimDetails.Where(cd => (cd.PolicyLinkLevel == null
                                                                                    && cd.DataId != claimDetail.DataId && cd.PolicyCoverageID == null));

                // Attaching all other Unattached ClaimDetails to the single Coverage reference by this claim detail
                foreach (ClaimDetail cd in allUnAttachedClaimDetails)
                {
                    cd.PolicyLinkLevel = claimDetail.PolicyLinkLevel;
                    cd.PolicySectionID = claimDetail.PolicySectionID;
                    cd.PolicySectionDetailID = claimDetail.PolicySectionDetailID;
                    cd.PolicyCoverageID = claimDetail.PolicyCoverageID;
                }
            }
        }

        /// <summary>
        /// Initialise Excess and 'is funded' values from Claim Header, if Automated Standard Deductible Processing is in place.
        /// </summary>
        /// <param name="claimDetail">Claim Detail</param>
		private void InitializeExcess(ClaimDetail claimDetail)
		{
            // Only process if the Automatic Deductible Processing Method is 'Standard Claim Detail Deductible', and we are applying Automatic Deducitble Processing.
			if (claimDetail.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible
				&& claimDetail.IsAutomaticDeductibleProcessingApplied == true)
			{
				ClaimHeader claimHeader = claimDetail.ClaimHeader;
                // Default the values for Excess, and whether or not it's funded, from the Claim Header.
                // CustomBoolean15 = Funded?
				claimDetail.IsDeductible01PaidByInsurer = claimHeader.CustomBoolean15;   // UI Label = Funded?; Only used for Motor Product - CGBIMO
                // CustomNumeric10 =Excess
				claimDetail.PolicyDeductible01 = claimHeader.CustomNumeric10;   // UI Label = Excess
			}
		}

        /// <summary>
        /// Sets the title on the Claim Detail passed in
        /// </summary>
        /// <param name="claimDetail">Claim Detail</param>
        /// <param name="point">Invocation Point Created</param>
        private void SetClaimDetailTitle(ClaimDetail claimDetail, ProcessInvocationPoint point)
        {
            // Find if there is an Additional Claimant or a Driver linked to this claim detail
            ClaimDetailToClaimInvolvementLink link = claimDetail.ClaimDetailToClaimInvolvementLinks.Where(
                x => x.ClaimInvolvement.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement
                && x.ClaimNameInvolvement != null
                && (x.ClaimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant
                || x.ClaimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.Driver)).FirstOrDefault();

            // If there is an Additional Claimant or a Driver linked, we set the title via the ClaimBusinessLogicHelper method.
            // Otherwise, the Claim Detail title is set to match the Claim Detail's type code.
            if (link!=null)
            {
                ClaimsBusinessLogicHelper.SetClaimDetailTitle(link, point);
            }
            else
            {
                claimDetail.ClaimDetailTitle = claimDetail.ClaimDetailTypeCode;
            }
        }

        /// <summary>
        /// Add ClaimDetail Inactivity Event if applicable.
        /// </summary>
        /// <param name="pluginHelper">ClaimDetail type PluginHelper</param>
        private void ClaimDetailStatusChangeEvent(PluginHelper<ClaimDetail> pluginHelper)
        {
            ClaimDetail claimDetail = pluginHelper.Component as ClaimDetail;
            if (!claimDetail.PropertiesChanged.Keys.Contains(ClaimDetail.ClaimDetailStatusCodeFieldName))
            {
                // The Claim Detail Status Code wasn't changed so no event to raise.
                return;
            }

            if (claimDetail.ClaimDetailStatusCode != ClaimConstants.CLAIM_DETAIL_CLOSED)
            {
                // Don't raise event if the Claim Detail is not set to Closed.
                return;
            }

            ClaimHeader claimHeader = claimDetail.ClaimHeader;
            IEnumerable<ClaimTransactionHeader> transactionHeader = claimHeader.HistoricalClaimTransactionHeaders;

            if (transactionHeader == null || transactionHeader.Count() == 0)
            {
                // If there are no Claim Transaction Headers associated with this claim then don't raise an event
                return;
            }

            // Retrieve the Inactivity Event Type Code from the application configuration
            string inactivityEventTypeCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(EstimateSettledEventTypeCode);
            // Checks if no Inactivity Events already for this Claim Detail AND there's a Reserve or a Recovery Reserve against it.
            // Adds the Inactivity Event if that's the case.
            if ((claimDetail.Events==null || !claimDetail.Events.Any(x=>x.EventTypeCode == inactivityEventTypeCode)) &&
                !Xiap.Claims.BusinessLogic.ClaimTransactionHelper.HasNonZeroReserve(claimDetail, (short)StaticValues.AmountType.Reserve, false) && 
                !Xiap.Claims.BusinessLogic.ClaimTransactionHelper.HasNonZeroReserve(claimDetail, (short)StaticValues.AmountType.RecoveryReserve, false))
            {
                claimDetail.AddNewClaimEvent(inactivityEventTypeCode, claimHeader, true);
            }
        }

        /// <summary>
        /// Add Review Event (Description "Estimate Reviewed - No Change")
        /// </summary>
        /// <param name="clmDetail">Claim Detail</param>
        private void CreateReviewEvent(ClaimDetail clmDetail)
        {
            if (clmDetail != null)
            {
                // Get the reviewed event type code from the application configuration, if it exists
                string eventTypeCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(EstimateReviewedEventTypeCode);
                if (eventTypeCode != null || eventTypeCode != string.Empty)
                {
                    // Find the first ProductEvent (if any) that matches the reviewed event type code from the config.
                    var productEvent = ProductService.GetProductEventQuery().GetProductEvents(clmDetail.ProductVersionID.GetValueOrDefault())
                                .Where(x => x.EventTypeCode == eventTypeCode).FirstOrDefault();

                    if (productEvent != null && productEvent.ProductEventID != 0)
                    {
                        // if we have a ProductEvent add the appropriate event to the claim detail.
                        ClaimEvent claimEvent = clmDetail.AddNewClaimEvent(productEvent.ProductEventID, true);
                        claimEvent.EventDescription = ClaimConstants.INACT_EVENT_DESC;
                    }
                }
            }
        }
    }
}
