using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class PolicyAttachmentAndDateProcessingVehicleTypeValidationPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Perform Validation for Coverage verifaction process for Motor Product
        /// </summary>
        /// <param name="component"> Claim Header</param>
        /// <param name="point">Process Invocation Point Virtual </param>
        /// <param name="pluginId">plugin Id</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimHeader> pluginHelper = new PluginHelper<ClaimHeader>(point, (ClaimHeader)component, new ProcessResultsCollection());

            if (point == ProcessInvocationPoint.Virtual)
            {
                this.PerformPreCoverageVerificationVehicleTypeValidations(pluginHelper);
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        ///  Add Mandatory Field validation
        /// </summary>
        /// <param name="pluginHelper">Plugin helper</param>
        private void PerformPreCoverageVerificationVehicleTypeValidations(PluginHelper<ClaimHeader> pluginHelper)
        {
            // Vehicle type if product is Motor
            if (string.IsNullOrWhiteSpace(pluginHelper.Component.ClaimHeaderAnalysisCode09))
            {
                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, MessageConstants.MANDATORY_FIELD_NOT_SPECIFIED, ProcessInvocationPoint.Virtual, pluginHelper.Component, pluginHelper.Component.ClaimHeaderAnalysisCode09Field.Title);
            }
        }
    }
}
