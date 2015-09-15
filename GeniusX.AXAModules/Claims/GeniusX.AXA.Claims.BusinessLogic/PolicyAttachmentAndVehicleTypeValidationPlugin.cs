using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework.Entity;
using Xiap.Framework.Metadata;
using Xiap.Metadata.BusinessComponent;
using Xiap.Framework.Extensions;
using Xiap.Framework.Data.Underwriting;
using Xiap.Metadata.Data.Enums;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class PolicyAttachmentAndVehicleTypeValidationPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// This will validate whether VehicleType and Class3Code of section detail of selected coverage are equal.
        /// </summary>
        /// <param name="component">Claim Header</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">plugin Id</param>
        /// <param name="processParameters">Process Parameters</param>
        /// <returns>ProcessResults Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
        {
            PluginHelper<ClaimHeader> pluginHelper = new PluginHelper<ClaimHeader>(point, component as ClaimHeader, new ProcessResultsCollection());

            switch (point)
            {
                case ProcessInvocationPoint.Virtual:
                    this.AreVehicleTypeAndGeniusVehicleCategoryEqual(pluginHelper, processParameters.Parameters[3].ToString(), processParameters.Parameters[4].ToString());
                    break;
            }


            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// This will validate whether VehicleType and Class3Code of section detail of selected coverage are equal.
        /// </summary>
        /// <param name="pluginHelper">Plugin Helper</param>
        /// <param name="sectionDetailClassificationCode03">section Detail Classification Code03</param>
        /// <param name="externalReference">External Reference</param>
        private void AreVehicleTypeAndGeniusVehicleCategoryEqual(PluginHelper<ClaimHeader> pluginHelper, string sectionDetailClassificationCode03, string externalReference)
        {
            ClaimHeader claimHeader = pluginHelper.Component;
            string vehicleTypeDescription = string.Empty;
            string geniusVehicleTypeDescription = string.Empty;

            if (claimHeader.ClaimHeaderAnalysisCode09 != sectionDetailClassificationCode03)
            {
                CodeRow vehicleTypeRow = pluginHelper.Component.ClaimHeaderAnalysisCode09Field.AllowedValues().Where(x => x.Code == pluginHelper.Component.ClaimHeaderAnalysisCode09).FirstOrDefault();
                CodeRow geniusVehicleTypeRow = pluginHelper.Component.ClaimHeaderAnalysisCode03Field.AllowedValues().Where(x => x.Code == sectionDetailClassificationCode03).FirstOrDefault();
                vehicleTypeDescription = vehicleTypeRow != null ? vehicleTypeRow.Description : pluginHelper.Component.ClaimHeaderAnalysisCode09;
                geniusVehicleTypeDescription = geniusVehicleTypeRow != null ? geniusVehicleTypeRow.Description : sectionDetailClassificationCode03;

                if (geniusVehicleTypeDescription == null)
                {
                    geniusVehicleTypeDescription = string.Empty;
                }

                pluginHelper.AddError(ClaimConstants.VEHICLE_TYPE_AND_GENIUS_VEHICLE_TYPE_MISMATCH, vehicleTypeDescription, geniusVehicleTypeDescription);
            }
        }
    }
}

