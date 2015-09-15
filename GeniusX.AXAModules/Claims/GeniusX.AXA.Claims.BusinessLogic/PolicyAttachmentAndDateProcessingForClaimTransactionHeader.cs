using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// If the Claim is attached and the Policy source is external — that is, Genius — this validates so that financial transactions can only be added if 
    /// the status of the Policy is Verified.
    /// Additionally, it validates to check financial transactions cannot be created if corresponding unauthorised transactions already exist and have 
    /// not yet been authorised.
    /// </summary>
    public class PolicyAttachmentAndDateProcessingForClaimTransactionHeader : AbstractComponentPlugin
    {
        /// <summary>
        /// Process for PreCreate only.
        /// </summary>
        /// <param name="component">Component of Business Type</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">unique plugin id</param>
        /// <returns>collection of process results</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimTransactionHeader> pluginHelper = new PluginHelper<ClaimTransactionHeader>(point, (ClaimTransactionHeader)component, new ProcessResultsCollection());
            
            switch (point)
            {
                case ProcessInvocationPoint.PreCreateValidation:
                    this.PreCreateValidation(pluginHelper);
                    this.PreCreateValidateClaimTransactionHeader(pluginHelper);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Raise an error on the basis of Policy Verification.
        /// </summary>
        /// <param name="pluginHelper">collection of process results</param>
        private void PreCreateValidation(PluginHelper<ClaimTransactionHeader> pluginHelper)
        {
            ClaimTransactionHeader claimTransHeader = (ClaimTransactionHeader)pluginHelper.Component;
            ClaimHeader claimHeader = claimTransHeader.ClaimHeader;

            // If the following is true: 
            //      1. The claim is attached to a policy
            //      2. Automatic Deductible processing is turned on for the claim
            //      3. The Policy isn't in the Verified state
            // Then raise an error that claim transactions aren't allowed with a non-verified policy attachment
            if (!string.IsNullOrEmpty(claimHeader.PolicyExternalDataSource) && claimTransHeader.ClaimHeader.PolicyHeaderID > 0)
            {
                if (claimHeader.IsAutomaticDeductibleProcessingApplied.GetValueOrDefault(false))
                {
                    if (!ClaimsBusinessLogicHelper.IsPolicyVerified(claimHeader))
                    {
                        ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.POLICYNOTVERIFIED_CLAIMTRANSACTION_NOTALLOWED, ProcessInvocationPoint.PreCreateValidation, claimTransHeader);
                    }
                }
            }
        }

        /// <summary>
        /// Raise an error if there is an unauthorised claim amount on reserve, payment, Recovery reserve or recovery receipts
        /// for this claim.
        /// </summary>
        /// <param name="pluginHelper">collection of process results</param>
        private void PreCreateValidateClaimTransactionHeader(PluginHelper<ClaimTransactionHeader> pluginHelper)
        {
            ClaimTransactionHeader claimTransHeader = pluginHelper.Component;
            if (claimTransHeader != null)
            {
                ClaimHeader claimHeader = claimTransHeader.ClaimHeader;
                if (claimHeader != null)
                {
                    StaticValues.ClaimTransactionSource claimTransactionSource = (StaticValues.ClaimTransactionSource)claimTransHeader.ClaimTransactionSource;
                    switch (claimTransactionSource)
                    {
                        case StaticValues.ClaimTransactionSource.Reserve:
                            if (ClaimsBusinessLogicHelper.HasUnauthorisedReserveAndPayment(claimHeader))
                            {
                                // if there is any unauthorised reserve or payment exists, add error.
                                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED, pluginHelper.InvocationPoint, pluginHelper.Component, StaticValues.ClaimTransactionSource.Reserve.ToString(), claimHeader.ClaimReference);
                            }

                            if (ClaimsBusinessLogicHelper.HasUnauthorisedRecoveryReceipt(claimHeader))
                            {
                                // if there is any unauthorised recovery receipt exists, add error.
                                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED, pluginHelper.InvocationPoint, pluginHelper.Component, StaticValues.ClaimTransactionSource.RecoveryReceipt.ToString(), claimHeader.ClaimReference);
                            }

                            break;
                        case StaticValues.ClaimTransactionSource.Payment:
                            if (ClaimsBusinessLogicHelper.HasUnauthorisedReserve(claimHeader))
                            {
                                // if there is any unauthorised reserve exists, add error.
                                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED, pluginHelper.InvocationPoint, pluginHelper.Component, StaticValues.ClaimTransactionSource.Reserve.ToString(), claimHeader.ClaimReference);
                            }

                            break;
                        case StaticValues.ClaimTransactionSource.RecoveryReserve:
                            if (ClaimsBusinessLogicHelper.HasUnauthorisedRecoveryReceipt(claimHeader))
                            {
                                // if there is any unauthorised recovery receipt exists, add error.
                                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED, pluginHelper.InvocationPoint, pluginHelper.Component, StaticValues.ClaimTransactionSource.RecoveryReceipt.ToString(), claimHeader.ClaimReference);
                            }

                            break;
                        case StaticValues.ClaimTransactionSource.RecoveryReceipt:
                            if (ClaimsBusinessLogicHelper.HasUnauthorisedReserve(claimHeader))
                            {
                                // if there is any unauthorised reserve exists, add error.
                                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED, pluginHelper.InvocationPoint, pluginHelper.Component, StaticValues.ClaimTransactionSource.Reserve.ToString(), claimHeader.ClaimReference);
                            }

                            break;
                    }
                }
            }
        }
    }
}
