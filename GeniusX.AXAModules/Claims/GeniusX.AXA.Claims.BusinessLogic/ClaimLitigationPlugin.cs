using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Validates that a Litigation can only be added when the Claim is at a certain Header status. 
    /// That is, it cannot be added if the status of the Header is Unconfirmed.
    /// </summary>
    public class ClaimLitigationPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Cal on ClaimLitigation
        /// </summary>
        /// <param name="component">Claim Litigation</param>
        /// <param name="point">Pre Create Validation</param>
        /// <param name="pluginId">Plug In ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            ClaimLitigation claimLitigation = (ClaimLitigation)component;
            PluginHelper<ClaimLitigation> pluginHelper = new PluginHelper<ClaimLitigation>(point, claimLitigation, new ProcessResultsCollection());
            switch (point)
            {
                case ProcessInvocationPoint.PreCreateValidation:
                    this.ValidateClaimHeaderStatus(pluginHelper);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Validate claimheaderstatus
        /// </summary>
        /// <param name="pluginHelper">ClaimLitigation type PlugIN Helper</param>
        private void ValidateClaimHeaderStatus(PluginHelper<ClaimLitigation> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component.Parent.Parent as ClaimHeader;
            if (ClaimsBusinessLogicHelper.ValidateClaimHeaderStatusCode(claimHeader, ClaimConstants.LITIGATION) == false)
            {
                pluginHelper.ProcessResults = ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.LITIGATION_OR_RECOVERY_NOT_ALLOWED, pluginHelper.InvocationPoint, pluginHelper.Component);
            }
        }
    }
}
