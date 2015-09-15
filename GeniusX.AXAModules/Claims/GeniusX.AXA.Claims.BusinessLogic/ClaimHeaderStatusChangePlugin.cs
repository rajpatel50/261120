using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Validates that Litigations and Recoveries can only be added when a certain status is reached and that the Notifier setting must be set on one 
    /// of the Name Involvements once a certain Header status is reached, for example, the Claim Open status.
    /// </summary>
    public class ClaimHeaderStatusChangePlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Call on claimheaderstatus change
        /// </summary>
        /// <param name="component">Claim Header</param>
        /// <param name="point">Point Validation</param>
        /// <param name="pluginId">PlugIN ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimHeader> pluginHelper = new PluginHelper<ClaimHeader>(point, (ClaimHeader)component, new ProcessResultsCollection());

            switch (point)
            {
                case ProcessInvocationPoint.Validation:
                    this.ValidateClaimHeaderStatus(pluginHelper);
                    this.NotifierValidation(pluginHelper);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Validate ClaimHeaderStatus
        /// </summary>
        /// <param name="pluginHelper">ClaimHeader type helper PlugIN </param>
        private void ValidateClaimHeaderStatus(PluginHelper<ClaimHeader> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component as ClaimHeader;
            if (ClaimsBusinessLogicHelper.ValidateClaimHeaderStatusCode(claimHeader, null) == false)
            {
                pluginHelper.ProcessResults = ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.LITIGATION_OR_RECOVERY_NOT_ALLOWED, pluginHelper.InvocationPoint, pluginHelper.Component);
            }
        }

        /// <summary>
        /// Add Error on IsNotiferFlagFoundMoreThanOne and IsNotiferFlagNotFound
        /// </summary>
        /// <param name="pluginHelper">ClaimHeader Type Helper Plugin</param>
        private void NotifierValidation(PluginHelper<ClaimHeader> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component as ClaimHeader;

            if (ClaimsBusinessLogicHelper.IsNotiferFlagFoundMoreThanOne(claimHeader) == true)
            {
                // MSG01
                pluginHelper.AddError(ClaimConstants.MANY_NOTIFIER_SPECIFIED_FOR_THE_CLAIM);
            }

            if (ClaimsBusinessLogicHelper.IsNotiferFlagNotFound(claimHeader) == true)
            {
                // MSG02
                pluginHelper.AddError(ClaimConstants.NOTIFIER_MUST_BE_SPECIFIED_FOR_THE_CLAIM);
            }
        }
    }
}
