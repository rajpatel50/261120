using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Validates that a Recovery can only be added when the Claim is at a certain Header status. 
    /// That is, it cannot be added if the status of the Header is Unconfirmed.
    /// </summary>
    public class ClaimRecoveryPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Valiate Claim Header Status on invocation point of PreCreateValidation.
        /// </summary>
        /// <param name="component">Component of Business Type</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">unique plugin id</param>
        /// <returns>collection of process results</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            ClaimRecovery claimRecovery = (ClaimRecovery)component;
            PluginHelper<ClaimRecovery> pluginHelper = new PluginHelper<ClaimRecovery>(point, claimRecovery, new ProcessResultsCollection());
            switch (point)
            {
                case ProcessInvocationPoint.PreCreateValidation:
                    this.ValidateClaimHeaderStatus(pluginHelper);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Validate the claim Header statuses and add error message on the basis of condition.
        /// </summary>
        /// <param name="pluginHelper">plugin helper of component type</param>
        private void ValidateClaimHeaderStatus(PluginHelper<ClaimRecovery> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component.Parent.Parent as ClaimHeader;
            if (ClaimsBusinessLogicHelper.ValidateClaimHeaderStatusCode(claimHeader, ClaimConstants.Recovery) == false)
            {
                pluginHelper.ProcessResults = ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.LITIGATION_OR_RECOVERY_NOT_ALLOWED, pluginHelper.InvocationPoint, pluginHelper.Component);
            }
        }
    }
}
