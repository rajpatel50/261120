using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Entity;
using Xiap.Framework.Messages;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class PolicyAttachmentAndTypeOfLossValidationPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// This will validate correct combination of TypeOfLoss and CoverageType through decision table.
        /// </summary>
        /// <param name="component">Claim Header Component</param>
        /// <param name="point">Process Invocation Point Virtual</param>
        /// <param name="pluginId">plugin Id</param>
        /// <param name="processParameters">List of processParameters</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
        {
            StaticValues.PolicyLinkLevel policyLinkLevel;
            PluginHelper<ClaimHeader> pluginHelper = new PluginHelper<ClaimHeader>(point, component as ClaimHeader, new ProcessResultsCollection());
            switch (point)
            {
                case ProcessInvocationPoint.Virtual:
                    this.ValidateTypeOfLossAndCoverageTypeCombination(pluginHelper, (string)processParameters.Parameters[2]);
                    if (Enum.TryParse(processParameters.Parameters[1].ToString(), out policyLinkLevel))
                    {
                        this.IsExternalPolicyChildComponentExists(pluginHelper, (string)processParameters.Parameters[0], policyLinkLevel);
                    }
                    else
                    {
                        string messageTitle = string.Empty;
                        string messageBody = string.Empty;
                        MessageServiceFactory.GetMessageService().GetMessage(MessageConstants.InvalidValue, out messageTitle, out messageBody, ClaimConstants.POLICY_LINK_LEVEL);
                        throw new ArgumentException(messageBody);
                    }

                    break;
            }

            return pluginHelper.ProcessResults;
        }


        /// <summary>
        /// This will validate correct combination of TypeOfLoss and CoverageType through decision table.
        /// </summary>
        /// <param name="pluginHelper">Plugin Helper</param>
        /// <param name="coverageTypeCode">Coverage Type Code</param>
        private void ValidateTypeOfLossAndCoverageTypeCombination(PluginHelper<ClaimHeader> pluginHelper, string coverageTypeCode)
        {
            ClaimHeader claimHeader = pluginHelper.Component;
            IDecisionTableHelper decisiontablehelper = ObjectFactory.Resolve<IDecisionTableHelper>();
            IDecisionTableComponent component = null;

            decisiontablehelper.TryCall(ClaimConstants.TYPE_OF_LOSS_AND_COVERAGE_TYPE_DECISION_TABLE_CODE, DateTime.Now, out component, claimHeader.ClaimHeaderAnalysisCode02, coverageTypeCode);

            if (component == null || !component.IsValid)
            {
                CodeRow row = claimHeader.ClaimHeaderAnalysisCode02Field.AllowedValues().Where(x => x.Code == claimHeader.ClaimHeaderAnalysisCode02).FirstOrDefault();
                string typeOfLossDescription = row != null ? row.Description : claimHeader.ClaimHeaderAnalysisCode02;
                string coverageTypeDescription = string.IsNullOrEmpty(coverageTypeCode) ? string.Empty : SystemValueSetCache.GetCodeDescription(coverageTypeCode, SystemValueSetCodeEnum.CoverageTypeCode, true);
                pluginHelper.AddError(ClaimConstants.TYPE_OF_LOSS_AND_COVERAGE_TYPE_MISMATCH, typeOfLossDescription, coverageTypeDescription);
            }
        }


        /// <summary>
        /// Check External component exist or not
        /// </summary>
        /// <param name="pluginHelper">Plugin Helper</param>
        /// <param name="externalReference">External Reference</param>
        /// <param name="linkLevel">Policy Link Level</param>
        public void IsExternalPolicyChildComponentExists(PluginHelper<ClaimHeader> pluginHelper, string externalReference, StaticValues.PolicyLinkLevel linkLevel)
        {
            if (string.IsNullOrWhiteSpace(externalReference))
            {
                throw new ArgumentException("External Reference should not be null or empty.");
            }

            bool externalComponentExists = false;

            IUnderwritingService uwService = ObjectFactory.Resolve<IUnderwritingService>();

            switch (linkLevel)
            {
                case StaticValues.PolicyLinkLevel.Coverage:
                    externalComponentExists = uwService.GetCoverageIDByExternalReference(externalReference) > 0;
                    break;

                case StaticValues.PolicyLinkLevel.Section:
                    externalComponentExists = uwService.GetSectionIDByExternalReference(externalReference) > 0;
                    break;

                case StaticValues.PolicyLinkLevel.SectionDetail:
                    externalComponentExists = uwService.GetSectionDetailIDByExternalReference(externalReference) > 0;
                    break;
            }

            if (!externalComponentExists)
            {
                pluginHelper.AddError(ClaimConstants.POLICY_ATTACHMENT_NOT_ALLOWED);
            }
        }
    }
}
