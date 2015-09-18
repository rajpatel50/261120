using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
	public class UKClaimsExternalPolicyVerification : AbstractComponentPlugin
	{
		private const string ConfigSetting_PolicyVerifiedHeaderStatus = "PolicyVerifiedHeaderStatus";
		/// <summary>
		/// Virtual plugin called by coverage verification to validate the external policy. Used by UK motor policies to check there is an existing Xuber policy for the
		/// Genius policy and that the policy is verified
		/// </summary>
		/// <param name="component">Claim Header</param>
		/// <param name="point">Points Create,ComponentChange</param>
		/// <param name="pluginId">PlugIN ID</param>
		/// <param name="processParameters">The parameters for the virtual process. This should contain a UwHeaderData for the external policy</param>
		/// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
	    public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
		{
			PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(point, (ClaimHeader)component, new ProcessResultsCollection());

			if (pluginHelper.InvocationPoint == ProcessInvocationPoint.Virtual)
			{
				UWHeaderData policyHeaderData = processParameters.Parameters != null && processParameters.Parameters.Any() ? processParameters.Parameters[0] as UWHeaderData : null;

				if (policyHeaderData == null || string.IsNullOrEmpty(policyHeaderData.HeaderReference))
				{
					ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.ATTACHMENT_NOT_ALLOWED, ProcessInvocationPoint.Virtual, pluginHelper.Component);
				}
				else
				{
					// Get the verified policy header status from the application configuration
					var verifiedHeaderStatus = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(ConfigSetting_PolicyVerifiedHeaderStatus);

					// check the header status matches the verified status
					if (policyHeaderData.HeaderStatusCode != verifiedHeaderStatus)
					{
						ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.POLICYNOTVERIFIED_COVERAGEVERIFICATION_NOTALLOWED, ProcessInvocationPoint.Virtual, pluginHelper.Component, policyHeaderData.HeaderReference);
					}
				}
			}

			return pluginHelper.ProcessResults;
		}
	}
}
