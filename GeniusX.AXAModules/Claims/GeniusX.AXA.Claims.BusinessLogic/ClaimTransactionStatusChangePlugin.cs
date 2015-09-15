using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

/// <summary>
/// /// Validates that if the Claim Header status is related to Report Only then payments or recovery receipts cannot exist. 
/// Additionally, it sets the Claim Header Status based on what financial transactions have been created on the Claim.
/// </summary>
namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ClaimTransactionStatusChangePlugin : ITransactionPlugin
    {
        #region ITransactionPlugin Members

        /// <summary>
        /// Transaction process started on Pre complete invocation point.
        /// </summary>
        /// <param name="businessTransaction">transaction of Business Type</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="PluginId">unique plugin id</param>
        /// <param name="parameters">Process parameters</param>
        /// <returns>collection of process results</returns>
        public ProcessResultsCollection ProcessTransaction(IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            if (point == TransactionInvocationPoint.PreComplete)
            {
                ClaimHeader claimHeader = (ClaimHeader)businessTransaction.Component;

                // If ClaimHeaderStatusCode is "Report Only-No Estimates" or "Report Only-Estimates Made" 
                // then raise errors if there are any payments or recovery receipts in progress
                if (claimHeader.ClaimHeaderStatusCode == "CRO" || claimHeader.ClaimHeaderStatusCode == "CRE")
                {
                    if (this.InProgressPaymentExists(claimHeader))
                    {
                        Xiap.Claims.BusinessLogic.ClaimsBusinessLogicHelper.AddError(businessTransaction.Results, "ENTRY_NOT_ALLOWED_FOR_REPORT_ONLY_CLAIMS", point, claimHeader, StaticValues.ClaimTransactionSource.Payment);
                    }
                    else if (this.InProgressRecoveryReceiptExists(claimHeader))
                    {
                        Xiap.Claims.BusinessLogic.ClaimsBusinessLogicHelper.AddError(businessTransaction.Results, "ENTRY_NOT_ALLOWED_FOR_REPORT_ONLY_CLAIMS", point, claimHeader, StaticValues.ClaimTransactionSource.RecoveryReceipt);
                    }
                }

                // If ClaimHeaderStatusCode is "Open - Payment Made" or "Open - Estimate Made" or "Open - No Estimate" or "Report Only - No Estimate"
                // then update the header status code based on payments or reserves.
                if (claimHeader.ClaimHeaderStatusCode == "CPY" || claimHeader.ClaimHeaderStatusCode == "CES" || claimHeader.ClaimHeaderStatusCode == "COU" || claimHeader.ClaimHeaderStatusCode == "CRO")
                {
                    // Check for any payments found on the claim.
                    if (this.IsPaymentFoundOnClaimTransactionHeaders(claimHeader))
                    {
                        claimHeader.ClaimHeaderStatusCode = "CPY";// Change to "Open - Payment Made" 
                        claimHeader.PropertiesChanged.Remove(ClaimHeader.ClaimHeaderStatusCodeFieldName);////fix to remove status validation error from ces to cpy                         
                    }                    
                    else if (this.IsReserveFoundOnClaimTransactionHeaders(claimHeader))
                    {
                        // Reserve Made and header status is "Report Only - No Estimate"
                        if (claimHeader.ClaimHeaderStatusCode == "CRO")
                        {
                            claimHeader.ClaimHeaderStatusCode = "CRE"; // change to "Report Only-Estimates Made" 
                        }
                        else
                        {
                            claimHeader.ClaimHeaderStatusCode = "CES"; // else change to "Open - Estimate Made"
                        }
                    }
                    else
                    {
                        // "Report Only - No Estimate"
                        if (claimHeader.ClaimHeaderStatusCode != "CRO")
                        {
                            // change to "Open - No Estimate"
                            claimHeader.ClaimHeaderStatusCode = "COU";
                        }
                    }
                }
            }

            return businessTransaction.Results;
        }

        #endregion

        /// <summary>
        /// Check if there is any payment in progress or authorised on this claim.
        /// </summary>
        /// <param name="claimHeader">component of Claim Header</param>
        /// <returns>bool value</returns>
        private bool IsPaymentFoundOnClaimTransactionHeaders(ClaimHeader claimHeader)
        {
            if (claimHeader.HistoricalClaimTransactionHeaders != null && claimHeader.HistoricalClaimTransactionHeaders.Where(a => a.ClaimTransactionSource.Value == (short)StaticValues.ClaimTransactionSource.Payment
                    && a.PaymentAuthorisationStatus != (short)StaticValues.PaymentAuthorisationStatus.PaymentRejected).Count() > 0)
            {
                return true;
            }
            else if (this.InProgressPaymentExists(claimHeader))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if there is any Reserve  in progress or authorised on this claim.
        /// </summary>
        /// <param name="claimHeader">component of Claim Header</param>
        /// <returns>bool value</returns>
        private bool IsReserveFoundOnClaimTransactionHeaders(ClaimHeader claimHeader)
        {
            if (claimHeader.HistoricalClaimTransactionHeaders != null && claimHeader.HistoricalClaimTransactionHeaders.Where(a => a.ClaimTransactionSource.Value == (short)StaticValues.ClaimTransactionSource.Reserve
                   && a.ReserveAuthorisationStatus != (short)StaticValues.ReserveAuthorisationStatus.ReserveRejected).Count() > 0)
            {
                return true;
            }
            else if (claimHeader.InProgressClaimTransactionHeaders != null && claimHeader.InProgressClaimTransactionHeaders.Where(a => a.ClaimTransactionSource.Value == (short)StaticValues.ClaimTransactionSource.Reserve
                   && a.ReserveAuthorisationStatus != (short)StaticValues.ReserveAuthorisationStatus.ReserveRejected).Count() > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if there is any payment in progress on this claim.
        /// </summary>
        /// <param name="claimHeader">component of Claim Header</param>
        /// <returns>bool value</returns>
        private bool InProgressPaymentExists(ClaimHeader claimHeader)
        {
            if (claimHeader.InProgressClaimTransactionHeaders != null && claimHeader.InProgressClaimTransactionHeaders.Where(a => a.ClaimTransactionSource.Value == (short)StaticValues.ClaimTransactionSource.Payment
                    && a.PaymentAuthorisationStatus != (short)StaticValues.PaymentAuthorisationStatus.PaymentRejected).Count() > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if there is any Recovery Receipt in progress on this claim
        /// </summary>
        /// <param name="claimHeader">component of Claim Header</param>
        /// <returns>bool value</returns>
        private bool InProgressRecoveryReceiptExists(ClaimHeader claimHeader)
        {
            if (claimHeader.InProgressClaimTransactionHeaders != null && claimHeader.InProgressClaimTransactionHeaders.Where(a => a.ClaimTransactionSource.Value == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt).Count() > 0)
            {
                return true;
            }

            return false;
        }
    }
}
