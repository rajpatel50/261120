using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.Extensions;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ClaimDetailChangeAttachmentPlugin : AbstractComponentPlugin
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
                // If we change a claim detail, enforce it is still attached to the same policy component.
                case ProcessInvocationPoint.ComponentChange:
                    if (pluginHelper.Component.PropertiesChanged.ContainsKey(ClaimConstants.POLICY_LINK_LEVEL))
                    {
                        ClaimsBusinessLogicHelper.ValidateStartAndEndDate(pluginHelper);
                    }

                    break;
            }

            return pluginHelper.ProcessResults;
        }
    }
}
