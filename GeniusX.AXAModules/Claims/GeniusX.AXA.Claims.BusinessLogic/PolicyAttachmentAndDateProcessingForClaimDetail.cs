using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class PolicyAttachmentAndDateProcessingForClaimDetail : AbstractComponentPlugin
    {
        private const string EstimateSettledEventTypeCode = "EstimateSettledEventTypeCode";
        private const string EstimateReviewedEventTypeCode = "EstimateReviewedEventTypeCode";

        /// <summary>
        /// Call on ClaimDetail Component
        /// </summary>
        /// <param name="component">Claim Detail</param>
        /// <param name="point">PreCreate Validation</param>
        /// <param name="pluginId">PlugIn ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>  
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimDetail> pluginHelper = new PluginHelper<ClaimDetail>(point, component as ClaimDetail, new ProcessResultsCollection());

            switch (point)
            {
                // Make sure we only bother to get E&D from the policy once, by checking for existing, similar claim details.
                case ProcessInvocationPoint.PreCreateValidation:
                    this.PreCreateValidate(pluginHelper);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Do PreCreate Validation
        /// Synopsis: Validate the XIAP policy 
        /// Extract of E&amp;D data will only occur once. If another uncancelled CD already exists which is linked to policy component
        /// then it is assumed that E&amp;D data has already been retrieved.
        /// </summary>
        /// <param name="pluginHelper">ClaimDetail type PluginHelper</param>
        private void PreCreateValidate(PluginHelper<ClaimDetail> pluginHelper)
        {
            ClaimDetail claimDetail = pluginHelper.Component;
            ClaimHeader claimHeader = claimDetail.ClaimHeader;
            // This check is finding an existing, non-cancelled or finalised Claim Detail, meaning policy is already validated for E&D
            if (claimHeader.ClaimDetails.Where(a => a.PolicyLinkLevel > 0 && a.DataId != claimDetail.DataId && a.ClaimDetailInternalStatus
                != (short?)StaticValues.ClaimDetailInternalStatus.ClosedCreatedinError && a.ClaimDetailInternalStatus != (short?)StaticValues.ClaimDetailInternalStatus.Finalized).Any())
            {
                return;
            }

            this.ValidateAXAPolicy(pluginHelper);
        }


        /// <summary>
        /// Validate AXAPolicy
        /// </summary>
        /// <param name="pluginHelper">Validate AXA Policy</param>
        /// <returns>return true if it is successfully validate</returns>
        private bool ValidateAXAPolicy(PluginHelper<ClaimDetail> pluginHelper)
        {
            // This validation ideally needs to run after the core PreCreateValidation which creates an XIAP policy if required
            ClaimDetail claimDetail = pluginHelper.Component;
            ClaimHeader claimHeader = claimDetail.ClaimHeader;
            // If Claim Detail is attached to a policy component, check policy is verified
            if ((claimDetail.UWSection != null || claimDetail.UWSectionDetail != null || claimDetail.UWCoverage != null)
                && !string.IsNullOrEmpty(claimHeader.PolicyExternalDataSource))
            {
                ProductClaimDefinition productClaimDef = claimHeader.GetProduct().ProductClaimDefinition;
                if (productClaimDef.ClaimHeaderAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.StandardClaimHeaderDeductible)
                {
                    if (!ClaimsBusinessLogicHelper.IsPolicyVerified(claimHeader))
                    {
                        ClaimsBusinessLogicHelper.AddFatalError(pluginHelper.ProcessResults, ClaimConstants.POLICYNOTVERIFIED_COVERAGEVERIFICATION_NOTALLOWED, ProcessInvocationPoint.PreCreateValidation, claimDetail, claimHeader.UWHeader.HeaderReference);
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
