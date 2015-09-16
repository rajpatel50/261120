using System;
using System.Configuration;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.DataMapping;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class AXAClaimDocumentPlugin : AbstractComponentPlugin
    {
        private const string ClaimEventDocumentReferenceCustomReferenceField = "ClaimEvent_DocumentReference_CustomReferenceField";
        private const string ClaimEventDocumentGroupReferenceCustomReferenceField = "ClaimEvent_DocumentGroupReference_CustomReferenceField";
        private const string ClaimEventGenerateTaskCustomCodeField = "ClaimEvent_GenerateTask_CustomCodeField";
        private const string ClaimDocumentGenerateTaskCustomCodeField = "ClaimDocument_GenerateTask_CustomCodeField";
        private static PrimitivePropertyAccessor propAccessorSet = new PrimitivePropertyAccessor(typeof(ClaimEvent));
        private static PrimitivePropertyAccessor propAccessorGet = new PrimitivePropertyAccessor(typeof(ClaimDocument));
        /// <summary>
        /// This plugin is mapping DocumentReference,DocumentGroupReference and CustomCode08 claim Event CustomReference01, CustomReference02 and CustomCode08 respectivelly
        /// </summary>
        /// <param name="component">Claim Document</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">plugin Id</param>
        /// <param name="processParameters">process Parameters</param>
        /// <returns>Process Results</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
        {
            var document = (ClaimDocument)component;
            var claimEvent = (ClaimEvent)processParameters.Parameters[0];
            PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(point, (ClaimDocument)component, new ProcessResultsCollection());
            switch (point)
            {
                case ProcessInvocationPoint.Virtual:
                    {
                        propAccessorSet.SetProperty(claimEvent, ConfigurationManager.AppSettings[ClaimEventDocumentReferenceCustomReferenceField], document.DocumentReference);
                        propAccessorSet.SetProperty(claimEvent, ConfigurationManager.AppSettings[ClaimEventDocumentGroupReferenceCustomReferenceField], document.DocumentGroupReference);
                        var generateTask = propAccessorGet.GetProperty(document, ConfigurationManager.AppSettings[ClaimDocumentGenerateTaskCustomCodeField]);
                        if (generateTask != null)
                        {
                            propAccessorSet.SetProperty(claimEvent, ConfigurationManager.AppSettings[ClaimEventGenerateTaskCustomCodeField], Convert.ToString(generateTask));
                        }

                        break;
                    }
            }

            return pluginHelper.ProcessResults;
        }
    }
}
