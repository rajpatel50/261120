using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// If the Claim Involvement is a Claim Name Involvement and it is for the Additional Claimant or Driver, 
    /// then it adds the ListName of the Name to the ClaimDetailTitle on the Claim Detail.
    /// </summary>
    public class ClaimDetailToClaimInvolvementLinkPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Call in ClaimDetailToClaimInvolvementLinkPlugin basically it set ClaimDetail title.
        /// </summary>
        /// <param name="component">ClaimDetail To ClaimInvolvementLink</param>
        /// <param name="point">Point Create,Deletes</param>
        /// <param name="pluginId">PlugIN ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
             PluginHelper<ClaimDetailToClaimInvolvementLink> pluginHelper = new PluginHelper<ClaimDetailToClaimInvolvementLink>(point, component as ClaimDetailToClaimInvolvementLink, new ProcessResultsCollection());
             switch (point)
             {
                 case ProcessInvocationPoint.Created:
                 case ProcessInvocationPoint.Delete:
                     ClaimsBusinessLogicHelper.SetClaimDetailTitle(pluginHelper.Component as ClaimDetailToClaimInvolvementLink, point);
                     break;
             }

             return pluginHelper.ProcessResults;
        }
    }
}
