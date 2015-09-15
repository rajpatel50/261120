using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.GeniusIntegration.BusinessLogic;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework;
using Xiap.Framework.Logging;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.Configuration;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Entity;
using Xiap.Claims.BusinessTransaction;
using Xiap.Framework.Common;
using Xiap.Framework.Extensions;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Creates entry in ClaimTransferControl log to transfer claim(s) to Genius
    /// </summary>
    public class AXAClaimTransferPlugin : ITransactionPlugin
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
       
        /// <summary>
        /// Process on the PostComplete point.
        /// If Create Claim,CloseClaim,ReopenClaim,AmendClaim,Copy,Delete ,CancelPayment,ReserveAuthorisation,PaymentAuthorisation,RecoveryReceiptAuthorisation
        /// </summary>
        /// <param name="businessTransaction">Processing ClaimTransactionHeader</param>
        /// <param name="point">it is invocation(PostComplete)</param>
        /// <param name="PluginId">Plugin ID</param>
        /// <param name="parameters">Process transaction Parameters if any </param>
        /// <returns>return Result Collection</returns>
        public ProcessResultsCollection ProcessTransaction(IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            ClaimHeader claimHeader = null;
            using (PerfLogger _LG = new PerfLogger(typeof(AXAClaimTransferPlugin), "AXAClaimTransferPlugin"))
            {
                if (point == TransactionInvocationPoint.PostComplete)
                {
                    if (businessTransaction.Component != null)
                    {
                        if (businessTransaction.Component is ClaimTransactionHeader)
                        {
                            claimHeader = businessTransaction.Component.Parent as ClaimHeader;
                        }
                        else
                        {
                            claimHeader = businessTransaction.Component as ClaimHeader;
                        }
                    }

                    // Get invalid Header Status codes from the application configuration
                    // No claims of this header status code will be transferred.
                    string invalidClaimHeaderStatusCodes = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(ClaimConstants.APP_SETTING_KEY_HEADERSTATUSESFORINVALIDCLAIMTRANSFER);
                    if (claimHeader != null && ClaimsBusinessLogicHelper.CheckValidHeaderStatus(claimHeader, invalidClaimHeaderStatusCodes))
                    {
                        if (_Logger.IsDebugEnabled)
                        {
                            _Logger.Debug(claimHeader.ClaimReference + " : has valid claim header status code.");
                        }

                        // If header status of claim has been changed to Abandoned then if previous header status was valid then insert entry in claim transfer control log table.
                        if (claimHeader.ClaimHeaderStatusCode == ClaimConstants.CLAIM_HEADER_STATUS_ABANDONED)
                        {
                            if (claimHeader.OriginalTransitionStatus == ClaimConstants.CLAIM_HEADER_STATUS_NO_ESTIMATE
                                || claimHeader.OriginalTransitionStatus == ClaimConstants.CLAIM_HEADER_STATUS_ESTIMATE_MADE
                                || claimHeader.OriginalTransitionStatus == ClaimConstants.CLAIM_HEADER_STATUS_PAYMENT_MADE)
                            {
                                this.AddClaimTransferControlLogEntry(businessTransaction);
                            }

                            // No further processing of an abandoned claim
                            return null;
                        }

                        // Check if status is moved from invalid to valid then write out all ClaimTransactions to Transfer log which were added when claim status was invalid.
                        List<string> invalidClaimHeaderStatusesList = invalidClaimHeaderStatusCodes.Split(',').ToList<string>();
                        if (!string.IsNullOrEmpty(invalidClaimHeaderStatusCodes)
                            && claimHeader.OriginalTransitionStatus != claimHeader.ClaimHeaderStatusCode
                            && invalidClaimHeaderStatusesList.Contains(claimHeader.OriginalTransitionStatus.ToString())
                            && !invalidClaimHeaderStatusesList.Contains(claimHeader.ClaimHeaderStatusCode.ToString()))
                        {
                            // If there is status transition from invalid to valid then this won't insert records for financial transactions instead 
                            // Stored Procedure will insert all the finanical transactions.
                            this.AddClaimTransferControlLogEntry(businessTransaction, false);
                            if (_Logger.IsDebugEnabled)
                            {
                                _Logger.Debug(claimHeader.ClaimReference + " : Using SP to to Inserting financial records that were made when claim was having invalid header status code into ClaimTransferControlLog table.");
                            }

                            // Use Stored Procedure to retroactively insert the necessary financial recors.
                            IAXAClaimsQuery query = ObjectFactory.Resolve<IAXAClaimsQuery>();
                            query.ExecuteClaimTransferControlLogSP(claimHeader.ClaimReference.ToString());
                        }
                        else
                        {
                            if (_Logger.IsDebugEnabled)
                            {
                                _Logger.Debug(claimHeader.ClaimReference + " : Inserting financial records of claim having valid header status code, into ClaimTransferControlLog table.");
                            }

                            // This will insert records for create/update claim and financial transaction if exist.
                            this.AddClaimTransferControlLogEntry(businessTransaction, true);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Entry of claims having invalid ClaimHeaderStatusCode will not be inserted into ClaimTransferControlLog table
        /// </summary>
        /// <param name="businessTransaction">IBusinessTransaction businessTransaction</param>
        /// <param name="insertFinancialTransactions">Insert Financial Transactions</param>
        /// <returns>ProcessResultsCollection businessTransaction.Results</returns>
        private ProcessResultsCollection AddClaimTransferControlLogEntry(IBusinessTransaction businessTransaction, bool insertFinancialTransactions = true)
        {
            ClaimHeader claimHeader = null;
            bool? isRootComponentClaimHeader = null;
            if (businessTransaction.Component != null)
            {
                ComponentStateChange claimHeaderComponentStateChange = null;
                // Determine if the root component is a Claim Header or a Claim Transaction Header and set boolean appropriately
                if (businessTransaction.Component is ClaimTransactionHeader)
                {
                    claimHeader = businessTransaction.Component.Parent as ClaimHeader;
                    isRootComponentClaimHeader = false;
                }
                else
                {
                    claimHeader = businessTransaction.Component as ClaimHeader;
                    isRootComponentClaimHeader = true;
                }

                if (claimHeader != null)
                {
                    // Get a list of changes to the component.
                    List<ComponentStateChange> componentStateChanges = (businessTransaction as AbstractEFBusinessTransaction).ComponentStateChanges;
                    if (claimHeader.PolicyHeaderID.GetValueOrDefault(0) == 0 || string.IsNullOrEmpty(claimHeader.PolicyExternalDataSource))
                    {
                        // If there's no external data source attached to the product data, we aren't going to transfer anything so leave
                        return businessTransaction.Results;
                    }

                    if (componentStateChanges.Count > 0)
                    {
                        ClaimDetail claimDetail = null;
                        // Add a single entry to ClaimTransferRequestLog of type claim if Claim Header,ClaimDetail and ClaimNameInvolvement exist in list.
                        List<ComponentStateChange> componentList = componentStateChanges.Where(x => x.Component is ClaimHeader || x.Component is ClaimDetail || x.Component is ClaimNameInvolvement).ToList();
                        if (!componentList.IsNullOrEmpty())
                        {
                            claimHeaderComponentStateChange = componentList.Where(x => x.Component is ClaimHeader).FirstOrDefault();
                            // Claim Header changes 
                            if (claimHeaderComponentStateChange != null)
                            {
                                // Single entry should be added if ComponentChangeType is deleted.
                                if (claimHeaderComponentStateChange.ComponentChangeType == ChangeType.Deleted)
                                {
                                    ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.Delete, claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, null);
                                    return businessTransaction.Results;
                                }
                                else if (claimHeader.Context.TransactionType == ClaimsProcessConstants.REOPENCLAIM)
                                {
                                    // Otherwise add a reopen if we are doing a reopen on the claim.
                                    ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.Reopen, claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, null);
                                }
                            }

                            List<ComponentStateChange> claimDetailComponentList = componentList.Where(x => (x.Component is ClaimDetail)).ToList();
                            // Claim Detail changes
                            if (!claimDetailComponentList.IsNullOrEmpty())
                            {
                                // Find the first claim detail on the list that is attached to a policy
                                ComponentStateChange stateChange = claimDetailComponentList.Where(x => (x.Component as ClaimDetail).PolicyLinkLevel != null).FirstOrDefault();
                                if (stateChange != null)
                                {
                                    claimDetail = stateChange.Component as ClaimDetail;
                                }
                            }

                            if (claimDetail == null)
                            {
                                // We don't have a valid claim detail (it's attached to a policy)
                                // now go back to our list and find the first name involvement change, if any
                                ComponentStateChange componentStateChange = componentList.Where(x => x.Component is ClaimNameInvolvement).FirstOrDefault();

                                if (componentStateChange != null)
                                {
                                    ClaimNameInvolvement claimNameInvolvement = componentStateChange.Component as ClaimNameInvolvement;
                                    ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.Claim, claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, null);
                                }
                                else
                                {
                                    ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.Claim, claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, null);
                                }
                            }
                            else
                            {
                                // Change to claim detail so send through a claim change request.
                                ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.Claim, claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, null);
                            }
                        }

                        if (insertFinancialTransactions)
                        {
                            // For each authorised entry in the list for a ClaimTransactionheader in ClaimTransferRequestLog
                            List<ComponentStateChange> claimTransactionheaderlist = componentStateChanges.Where(x => x.Component is ClaimTransactionHeader).ToList();
                            if (!claimTransactionheaderlist.IsNullOrEmpty())
                            {
                                if (claimTransactionheaderlist.Any(x => (x.Component as ClaimTransactionHeader).IsClaimPaymentCancelled == true))
                                {
                                    // Remove cancelled payments from the list
                                    claimTransactionheaderlist.RemoveAll(x => (x.Component as ClaimTransactionHeader).ClaimTransactionSource != (short)StaticValues.ClaimTransactionSource.PaymentCancellation);
                                }

                                foreach (ComponentStateChange componentStateChange in claimTransactionheaderlist)
                                {
                                    ClaimTransactionHeader claimTransactionHeader = componentStateChange.Component as ClaimTransactionHeader;
                                    if (this.IsAmountAuthorized(isRootComponentClaimHeader, claimTransactionHeader))
                                    {
                                        // Add a transfer for any authorised payment
                                        ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.FinancialTransaction, claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, claimTransactionHeader.ClaimTransactionHeaderID);
                                    }
                                }
                            }
                        }

                        if (!componentList.IsNullOrEmpty())
                        {
                            // If we have any entry on the component list for a claim header
                            claimHeaderComponentStateChange = componentList.Where(x => x.Component is ClaimHeader).FirstOrDefault();
                            if (claimHeaderComponentStateChange != null)
                            {
                                claimHeader = claimHeaderComponentStateChange.Component as ClaimHeader;
                                // if we moved this claim to finalised or Closed/Opened in error, then send through a Close request to Genius.
                                if ((claimHeader.ClaimHeaderInternalStatus.GetValueOrDefault() == (short)StaticValues.ClaimHeaderInternalStatus.Finalized || claimHeader.ClaimHeaderInternalStatus.GetValueOrDefault() == (short)StaticValues.ClaimHeaderInternalStatus.ClosedOpenedInError) && claimHeader.ClaimHeaderStatusCode != claimHeader.OriginalTransitionStatus)
                                {
                                    ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.Close, claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, null);
                                }
                            }
                        }
                    }
                }
            }

            return businessTransaction.Results;
        }

        /// <summary>
        /// Is this an authorised amount on the ClaimTransactionHeader
        /// </summary>
        /// <param name="isRootComponentClaimHeader">Root Component Header</param>
        /// <param name="claimTransactionHeader">Processing Claim Transaction Header</param>
        /// <returns>return true if Authorize otherwise false</returns>
        private bool IsAmountAuthorized(bool? isRootComponentClaimHeader, ClaimTransactionHeader claimTransactionHeader)
        {
            // Return false for claim transaction headers that are in progress.
            if (isRootComponentClaimHeader.HasValue && isRootComponentClaimHeader.Value && !claimTransactionHeader.IsInProgress)
            {
                return false;
            }

            // Do we already have a transfer item for this Claim Transaction Header? If so, return false to stop us creating a new one.
            if (Xiap.Framework.Common.ClaimTransfer.IsClaimTransferControlLogExists(claimTransactionHeader.ClaimTransactionHeaderID))
            {
                return false;
            }

            short? transactionSource = claimTransactionHeader.ClaimTransactionSource;

            if ((transactionSource == (short)StaticValues.ClaimTransactionSource.Payment && claimTransactionHeader.PaymentAuthorisationStatus == (short)StaticValues.PaymentAuthorisationStatus.PaymentAuthorised)
                || (transactionSource == (short)StaticValues.ClaimTransactionSource.Reserve && claimTransactionHeader.ReserveAuthorisationStatus == (short)StaticValues.ReserveAuthorisationStatus.ReserveAuthorised)
                || (transactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReserve && claimTransactionHeader.RecoveryReserveAuthorisationStatus == (short)StaticValues.RecoveryReserveAuthorisationStatus.RecoveryReserveAuthorised)
                || (transactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt && claimTransactionHeader.RecoveryReceiptAuthorisationStatus == (short)StaticValues.RecoveryReceiptAuthorisationStatus.RecoveryReceiptAuthorised)
                || (transactionSource == (short)StaticValues.ClaimTransactionSource.PaymentCancellation && claimTransactionHeader.PaymentAuthorisationStatus == (short)StaticValues.PaymentAuthorisationStatus.PaymentAuthorised))
            {
                // if this is an authorised item.
                return true;
            }

            return false;
        }
    }
}
