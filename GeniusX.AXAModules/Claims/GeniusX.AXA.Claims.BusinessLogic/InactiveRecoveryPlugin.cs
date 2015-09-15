using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common.Product;
using Xiap.Framework.Security;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Creates Inactivity Review event on Claim Detail based on AXA functional specification
    /// </summary>
    public class InactiveRecoveryPlugin : AbstractComponentPlugin
    {
        private const string RecoveryEstimateReviewedEventTypeCode = "RecoveryEstimateReviewedEventTypeCode";

        /// <summary>
        /// Invocation point of plugin 
        /// </summary>
        /// <param name="component">Claim Detail component</param>
        /// <param name="point"> Invocation point Virtual</param>
        /// <param name="pluginId">Plugin Id </param>
        /// <returns>Result collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimDetail> pluginHelper = new PluginHelper<ClaimDetail>(point, component as ClaimDetail, new ProcessResultsCollection());

            switch (point)
            {
                case ProcessInvocationPoint.Virtual:
                    ClaimDetail cd = pluginHelper.Component as ClaimDetail;
                    this.CreateReviewEvent(cd);
                    break;
            }

            return pluginHelper.ProcessResults;
        }


     
        /// <summary>
        /// Create an Inactivity event for the selected claim Detail for event type -RESTRV
        /// </summary>
        /// <param name="clmDetail">Claim Detail</param>
        private void CreateReviewEvent(ClaimDetail clmDetail)
        {
            //// Create an Inactivity event for the selected claim Detail
            if (clmDetail != null)
            {
                string eventTypeCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(RecoveryEstimateReviewedEventTypeCode);
                if (eventTypeCode != null || eventTypeCode != string.Empty)
                {
                    var productEvent = ProductService.GetProductEventQuery().GetProductEvents(clmDetail.ProductVersionID.GetValueOrDefault())
                                .Where(x => x.EventTypeCode == eventTypeCode).FirstOrDefault();

                    if (productEvent != null && productEvent.ProductEventID != 0)
                    {
                        ClaimEvent claimEvent = clmDetail.AddNewClaimEvent(productEvent.ProductEventID, true);
                        claimEvent.EventDescription = ClaimConstants.REC_INACT_EVENT_DESC;
                    }
                }
            }
        }
    }
}
