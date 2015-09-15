using System.Collections.Generic;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Logging;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class LoadDeductibleReasonCodes : AbstractComponentPlugin
    {
        private static readonly ILogger _logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Processes the component on the Virtual invocation point.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="point">The point.</param>
        /// <param name="pluginId">The plugin identifier.</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimHeader> pluginHelper = new PluginHelper<ClaimHeader>(point, (ClaimHeader)component, new ProcessResultsCollection());
            if (point == ProcessInvocationPoint.Virtual)
            {
                this.LoadReasonCodes(pluginHelper);
            }

            return pluginHelper.ProcessResults;
        }
        /// <summary>
        /// Search the policy deductibles and excess records to see if there are any deductible reason codes 
        /// and attach them to the Transaction context in the form of the new DeductibleReasonCode data class.
        /// </summary>
        /// <param name="pluginHelper">Plugin Helper</param>
        private void LoadReasonCodes(PluginHelper<ClaimHeader> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component;
            // Only process if our Policy Product links to an external system (e.g. Genius) 
            // AND a Deductible Reason Code (ClaimHeaderAnalysisCode08) isn't already set
            if (!string.IsNullOrEmpty(claimHeader.PolicyExternalDataSource) && string.IsNullOrEmpty(claimHeader.ClaimHeaderAnalysisCode08))
            {
                ProductClaimDefinition productClaimDef = claimHeader.GetProduct().ProductClaimDefinition;
                // Check if we have Automatic Deductible Processing on the claim header set as Standard
                if (productClaimDef.ClaimHeaderAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.StandardClaimHeaderDeductible)
                {
                    IAXAClaimsQuery query = new AXAClaimsQueries();
                    // Get the reason codes for the Generic Data Type of "AND2" (Deductible) using the Policy Reference
                    Dictionary<string, List<CodeRow>> reasonCodes = query.GetPolicyReasonCodes(claimHeader.ProposedPolicyReference, "AND2");
                    if (reasonCodes.Count == 0)
                    {
                        // No Deductible Reason Codes found? Check for reason codes on AD Excess (AND3)
                        reasonCodes = query.GetPolicyReasonCodes(claimHeader.ProposedPolicyReference, "AND3");
                    }

                    if (reasonCodes.Count > 0)
                    {
                        // Add to the transaction context for the claim header.
                        DeductibleReasonCode deductibleReasonCode = new DeductibleReasonCode(reasonCodes);
                        claimHeader.Context.AddAttachedData(deductibleReasonCode);
                        claimHeader.Context.RegisterComponentChange(claimHeader.DataId, Xiap.Framework.Data.BusinessDataState.Modified);
                    }
                }
            }
        }
    }
}
