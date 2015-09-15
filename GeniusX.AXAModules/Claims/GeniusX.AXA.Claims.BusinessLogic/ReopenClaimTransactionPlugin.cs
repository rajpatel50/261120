using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Logging;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// If on the Claim Header field, CMSMigrationStatus (CustomCode18) is blank and Data Source (CustomCode19) is C,
    /// an error is added that prevents the Claim from being Re-opened.
    /// </summary>
    public class ReopenClaimTransactionPlugin : ITransactionPlugin
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// Add Claim wake up Validation
        /// </summary>
        /// <param name="businessTransaction">Bussiness Transaction Reopen Claim</param>
        /// <param name="point">Pre Creat</param>
        /// <param name="PluginId">Plugin Id</param>
        /// <param name="parameters">Extra Parameters</param>
        /// <returns>Process Results Collection</returns>
        public ProcessResultsCollection ProcessTransaction(IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            switch (point)
            {
                case TransactionInvocationPoint.PreCreate:
                    this.ValidateMigrationStatus(businessTransaction, point);
                    break;
            }

            return businessTransaction.Results;
        }

        /// <summary>
        /// Check whether migration processing has been completed or not.
        /// </summary>
        /// <param name="businessTransaction">IBusiness Transaction</param>
        /// <param name="point">Transaction Invocation Point</param>
        private void ValidateMigrationStatus(IBusinessTransaction businessTransaction, TransactionInvocationPoint point)
        {
            ClaimHeader claimHeader = (ClaimHeader)businessTransaction.Component;

            // UI Label = Data Source
            if (claimHeader.CustomCode19 == ClaimConstants.CLAIMS_MIGRATION_STATUS)   
            {
                // ClaimWakeUp Validations
                // CustomCode18=CMS Migration Status
                // UI Label = CMS Migration Status
                // if REOPENING OF UNPROCESSED CLAIM NOT ALLOWED
                // else if MIGRATED CLOSED CLAIM BEING PROCESSED
                // send message REOPENING OF A CLAIM ALREADY BEING PROCESSED NOT ALLOWED
                // else if = FAILED DUE TO INTERNAL SERVICE CONNECTION ISSUES
                // send message REOPENING OF A CLAIM THAT FAILED PROCESSING NOT ALLOWED
                // else if FAILED POLICY DOES NOT EXIST IN GENIUSX
                // send message REOPENING OF A CLAIM NOT ALLOWED WITHOUT POLICY
                // else if FAILED POLICY EXISTS BUT COULD NOT ATTACH TO CLAIM
                // send message REOPENING OF CLAIM NOT ALLOWED POLICY FAILURE

                if (string.IsNullOrWhiteSpace(claimHeader.CustomCode18))   
                {
                    ClaimsBusinessLogicHelper.AddFatalError(businessTransaction.Results, ClaimConstants.REOPENING_OF_UNPROCESSED_CLAIM_NOT_ALLOWED,point, businessTransaction.Component);
                }
                else if (claimHeader.CustomCode18 == ClaimConstants.MIGRATED_CLOSED_CLAIM_BEING_PROCESSED)   
                {
                    ClaimsBusinessLogicHelper.AddFatalError(businessTransaction.Results, ClaimConstants.REOPENING_OF_A_CLAIM_ALREADY_BEING_PROCESSED_NOT_ALLOWED, point, businessTransaction.Component);
                }
                else if (claimHeader.CustomCode18 == ClaimConstants.FAILED_DUE_TO_INTERNAL_SERVICE_CONNECTION_ISSUES)   
                {
                    ClaimsBusinessLogicHelper.AddFatalError(businessTransaction.Results, ClaimConstants.REOPENING_OF_A_CLAIM_THAT_FAILED_PROCESSING_NOT_ALLOWED, point, businessTransaction.Component);
                }
                else if (claimHeader.CustomCode18 == ClaimConstants.FAILED_POLICY_DOES_NOT_EXIST_IN_GENIUSX)   
                {
                    ClaimsBusinessLogicHelper.AddFatalError(businessTransaction.Results, ClaimConstants.REOPENING_OF_CLAIM_NOT_ALLOWED_WITHOUT_POLICY, point, businessTransaction.Component);
                }
                else if (claimHeader.CustomCode18 == ClaimConstants.FAILED_POLICY_EXISTS_BUT_COULD_NOT_ATTACH_TO_CLAIM)   
                {
                    ClaimsBusinessLogicHelper.AddFatalError(businessTransaction.Results, ClaimConstants.REOPENING_OF_CLAIM_NOT_ALLOWED_POLICY_FAILURE, point, businessTransaction.Component);
                }
            }
        }
    }
}
