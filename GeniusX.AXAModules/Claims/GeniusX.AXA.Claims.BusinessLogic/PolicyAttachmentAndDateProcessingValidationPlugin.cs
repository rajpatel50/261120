using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.Data;
using Xiap.Framework;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class PolicyAttachmentAndDateProcessingValidationPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Perform Validation for Coverage verifaction process
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
                this.PerformPreCoverageVerificationCustomValidations(pluginHelper);
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        ///  Add Mandatory Field validation
        /// </summary>
        /// <param name="pluginHelper">Plugin helper</param>
        private void PerformPreCoverageVerificationCustomValidations(PluginHelper<ClaimHeader> pluginHelper)
        {
            // Major Insured Mandatory
            ClaimNameInvolvement claimNameInvolvement = (ClaimNameInvolvement)pluginHelper.Component.ChildClaimNameInvolvements.Where(cn => cn.NameInvolvementType == (short?)StaticValues.NameInvolvementType_ClaimNameInvolvement.MajorInsured && cn.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();
            if (claimNameInvolvement == null)
            {
                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, MessageConstants.MANDATORY_FIELD_NOT_SPECIFIED, ProcessInvocationPoint.Virtual, pluginHelper.Component, ClaimConstants.MAJOR_INSURED_TITLE);
            }

            // Division
            if (string.IsNullOrWhiteSpace(pluginHelper.Component.ClaimHeaderAnalysisCode04) && pluginHelper.Component.ClaimHeaderAnalysisCode04Field != null && pluginHelper.Component.ClaimHeaderAnalysisCode04Field.LookupParameters.GroupCode != ClaimConstants.EmptyGroup)
            {
                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, MessageConstants.MANDATORY_FIELD_NOT_SPECIFIED, ProcessInvocationPoint.Virtual, pluginHelper.Component, pluginHelper.Component.ClaimHeaderAnalysisCode04Field.Title);
            }


            if (pluginHelper.Component.ClaimDetails.Count == 0 || pluginHelper.Component.ClaimDetails.Any(cd => cd.PolicyLinkLevel == null || cd.PolicyLinkLevel == 0) == false)
            {
                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.AT_LEAST_ONE_UNATTACHED_CLAIM_DETAIL_MUST_EXISTS, ProcessInvocationPoint.Virtual, pluginHelper.Component);
            }
        }
    }
}
