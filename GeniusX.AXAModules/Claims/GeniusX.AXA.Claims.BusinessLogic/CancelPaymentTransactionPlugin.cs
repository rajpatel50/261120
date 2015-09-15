using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Logging;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class CancelPaymentTransactionPlugin : ITransactionPlugin
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Adds error details to the transaction if the payment cannot be cancelled because unauthorised reserves,
        /// payments, or recovery receipts still exist on it.
        /// invoked when CancelPayment transaction is selected. 
        /// </summary>
        /// <param name="businessTransaction">TransactionType CancelPayment</param>
        /// <param name="point">Pre Create</param>
        /// <param name="PluginId">Plug ID</param>
        /// <param name="parameters">Process Transaction Parameters</param>
        /// <returns>return Result Collection</returns>
        public ProcessResultsCollection ProcessTransaction(IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            if (point == TransactionInvocationPoint.PreCreate)
            {
                PreValidate(businessTransaction);
            }

            return businessTransaction.Results;
        }

        /// <summary>
        /// It validate CancelPayment Transaction for UnauthorisedReserve,UnauthorisedPayment,UnauthorisedRecoveryReceipt
        /// </summary>
        /// <param name="businessTransaction">Claim Transaction Header</param>
        private static void PreValidate(Xiap.Framework.BusinessTransaction.IBusinessTransaction businessTransaction)
        {
            ClaimTransactionHeader claimTransHeader = (ClaimTransactionHeader)businessTransaction.Component;
            ClaimHeader claimHeader = claimTransHeader.ClaimHeader;
            if (claimHeader != null)
            {
                StaticValues.ClaimTransactionSource claimTransactionSource = (StaticValues.ClaimTransactionSource)claimTransHeader.ClaimTransactionSource;
                switch (claimTransactionSource)
                {
                    case StaticValues.ClaimTransactionSource.Payment:
                        if (ClaimsBusinessLogicHelper.HasUnauthorisedReserve(claimHeader))
                        {
                            ClaimsBusinessLogicHelper.AddFatalError(businessTransaction.Results, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED, TransactionInvocationPoint.PreCreate, businessTransaction.Component, StaticValues.ClaimTransactionSource.Reserve.ToString(), claimHeader.ClaimReference);
                        }

                        if (ClaimsBusinessLogicHelper.HasUnauthorisedPayment(claimHeader))
                        {
                            ClaimsBusinessLogicHelper.AddFatalError(businessTransaction.Results, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED, TransactionInvocationPoint.PreCreate, businessTransaction.Component, StaticValues.ClaimTransactionSource.Payment.ToString(), claimHeader.ClaimReference);
                        }

                        if (ClaimsBusinessLogicHelper.HasUnauthorisedRecoveryReceipt(claimHeader))
                        {
                            ClaimsBusinessLogicHelper.AddFatalError(businessTransaction.Results, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED, TransactionInvocationPoint.PreCreate, businessTransaction.Component, StaticValues.ClaimTransactionSource.RecoveryReceipt.ToString(), claimHeader.ClaimReference);
                        }

                        break;
                }
            }
        }
    }
}
