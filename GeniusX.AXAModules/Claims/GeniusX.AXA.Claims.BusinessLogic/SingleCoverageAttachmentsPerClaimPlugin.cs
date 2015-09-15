using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.Extensions;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class SingleCoverageAttachmentsPerClaimPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Called on ClaimDetail Change Attachment. If we are creating a Claim Detail and one already exists, copy the details from there
        /// to prevent another call to Coverage Verification. If we are updating
        /// </summary>
        /// <param name="component">Claim Detail</param>
        /// <param name="point">ComponentChange, Create, Validation</param>
        /// <param name="pluginId">PlugIN ID</param>
        /// <returns>return Result Collection </returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimDetail> pluginHelper = new PluginHelper<ClaimDetail>(point, component as ClaimDetail, new ProcessResultsCollection());

            ClaimDetail clmDetail = component as ClaimDetail;
           
            switch (point)
            {
                // If another claim detail already exists, use the same Coverage Verification settings to avoid checking policy on Genius again.
                case ProcessInvocationPoint.Created:
                    this.LinkPolicytoClaimDetails(pluginHelper);
                    ClaimsBusinessLogicHelper.ValidateStartAndEndDate(pluginHelper);
                    break;

                // If we change a claim detail, enforce it is still attached to the same policy component.
                case ProcessInvocationPoint.ComponentChange:
                    if (pluginHelper.Component.PropertiesChanged.ContainsKey(ClaimConstants.POLICY_LINK_LEVEL))
                    {
                        ClaimsBusinessLogicHelper.ValidateAgainstExistingClaimDetails(pluginHelper);
                        ClaimsBusinessLogicHelper.ValidateStartAndEndDate(pluginHelper);
                    }

                    break;

                // Validates that the Claim Detail is always attached to the same Policy Component.
                case ProcessInvocationPoint.Validation:
                    ClaimsBusinessLogicHelper.ValidateAgainstExistingClaimDetails(pluginHelper);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Make sure a new Claim Detail links to the same Policy Component as an existing Claim Detail
        /// which avoids another call to Coverage Verification.
        /// </summary>
        /// <param name="pluginHelper">ClaimDetail type PlugIN</param>
        private void LinkPolicytoClaimDetails(PluginHelper<ClaimDetail> pluginHelper)
        {
            ClaimDetail claimDetail = pluginHelper.Component as ClaimDetail;
            ClaimHeader clmHeader = claimDetail.ClaimHeader;

            // Check if the Claim Detail is attached to a Policy component and validate it's correctly attached, if so.
            if (claimDetail.OriginalClaimDetailDataID == null && claimDetail.PolicyLinkLevel != null)
            {
                ClaimsBusinessLogicHelper.ValidateAgainstExistingClaimDetails(pluginHelper);
                return;
            }

            // claim Detail Change of Attachment Check: the Claim Detail isn't attached to a policy component. If there's an existing
            // claim detail, we attach to the same component.
            if (claimDetail.OriginalClaimDetailDataID == null && claimDetail.PolicyLinkLevel == null)
            {
                // Find the first current (not closed/cancelled) Claim Detail that is attached to a policy component.
                ClaimDetail attachedClaimDetail = clmHeader.ClaimDetails.Where(cd => cd.ClaimDetailInternalStatus != (short)StaticValues.ClaimDetailInternalStatus.ClosedCreatedinError
                    && cd.PolicyLinkLevel != null).FirstOrDefault();

                // Attach this claim detail to the existing attached claim detail we found on the current claim
                if (attachedClaimDetail != null)
                {
                    claimDetail.PolicySectionID = attachedClaimDetail.PolicySectionID;
                    claimDetail.PolicySectionDetailID = attachedClaimDetail.PolicySectionDetailID;
                    claimDetail.PolicyCoverageID = attachedClaimDetail.PolicyCoverageID;
                    claimDetail.PolicyLinkLevel = attachedClaimDetail.PolicyLinkLevel;
                }
            }
        }
    }
}
