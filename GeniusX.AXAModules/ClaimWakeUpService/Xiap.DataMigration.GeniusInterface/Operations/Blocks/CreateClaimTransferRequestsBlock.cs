using Xiap.Framework;
using Xiap.Metadata.BusinessComponent;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Claims.BusinessComponent;
    using Entities;
    using ExcessAndDeductibles;
    using Framework.Common;
    using Metadata.Data.Enums;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json.Linq;
    using log4net;

    [BlockDescription("Create Claim Transfer requests", Description = "")]
    public class CreateClaimTransferRequestsBlock : IBlock
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CreateClaimTransferRequestsBlock));
        private static readonly string[] InvalidCodes = new[]
                                                 {
                                                     "CRE","CRL","CRO"
                                                 };

        public static Claim Execute(Claim claim)
        {
            var sw = Stopwatch.StartNew();
            try
            {

                if (!ShouldExecute(claim) ) return claim;
                var transaction =  claim.CreateAmendClaimWithoutValidationTransaction();
                
                ClaimHeader claimHeader = transaction.ClaimHeader;
                if (claimHeader == null && InvalidCodes.Contains(claimHeader.ClaimHeaderStatusCode))
                {
                    transaction.Cancel();
                    return claim;
                }

                // Transfer to Genius 
                CreateClaimTransferRequests(claimHeader, claim);
                transaction.Cancel();

                claim.ClaimTransfered = true;
                return claim;
            }
            finally
            {
                sw.Stop();
                var workDone = GlobalClaimWakeUp.Statistics.GetOrAdd(typeof(CreateClaimTransferRequestsBlock).Name, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                GlobalClaimWakeUp.Statistics[typeof(CreateClaimTransferRequestsBlock).Name] = (workDone + TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
            

        }

        private static bool ShouldExecute(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            if (config.ReopeningClaims)
                return config.TaskIsEnabled[typeof(CreateClaimTransferRequestsBlock)] &&
                (!claim.ClaimIsDuplicate && claim.ClaimIsClosed && !claim.ClaimProcessingFailed && !claim.ClaimAlreadyProcessed);

            return config.TaskIsEnabled[typeof(CreateClaimTransferRequestsBlock)] && 
                (!claim.ClaimIsDuplicate && !claim.ClaimIsClosed && !claim.ClaimProcessingFailed && !claim.ClaimAlreadyProcessed);
        }

        private static void CreateClaimTransferRequests(ClaimHeader claimHeader, Claim claim)
        {
            // Change to claim header and detail will automatically result in the claim transferring, need to transfer the latest reserve 
            // and names that aren't already in Genius

            try
            {
                // Claim Transfer Type: 1(Reopen), 2(Claim), 3(Financial FlattenedTransaction), 4(Close), 5(Delete)
                //long? lastReserveHeader = null;
                
                ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.Claim,
                                                   claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, null);
                Logger.InfoFormat("Claim transfer request created\r\n{0}\r\n", JObject.FromObject(new { claim.ClaimReference, claim.PolicyNumber }));
                var transactions = claimHeader.GetFlattenedTransactionData().ToArray();
                var latestReservesHeaders = (
                                            from t in transactions
                                            //from ctg in cth.TransactionGroups
                                            //from ctd in ctg.TransactionDetails
                                            where
                                                (t.AmountType == (short)StaticValues.AmountType.Reserve || t.AmountType == (short)StaticValues.AmountType.RecoveryReserve) &&
                                                (
                                                t.ClaimTransactionSource != (short)StaticValues.ClaimTransactionSource.Payment &&
                                                t.ClaimTransactionSource != (short)StaticValues.ClaimTransactionSource.PaymentCancellation &&
                                                t.ClaimTransactionSource != (short)StaticValues.ClaimTransactionSource.RecoveryReceipt
                                                )
                                            group t by new
                                            {
                                                t.ClaimDetailID,
                                                t.MovementType
                                            } into grp
                                            select new
                                                    {
                                                        grp.Key.ClaimDetailID,
                                                        ClaimTransactionHeaderID = 
                                                        grp.OrderByDescending(x => x.TransactionDate)
                                                            .ThenByDescending(x => x.ReserveDate)
                                                            .ThenByDescending(x => x.ReserveDaySequence)
                                                            .Select(x => x.ClaimTransactionHeaderID)
                                                            .First()
                                                    }
                                             ).ToArray();

                var latestReservesHeaderIds = latestReservesHeaders.Select(g => g.ClaimTransactionHeaderID).Distinct().ToArray();
                Logger.InfoFormat("Latest reserves for Claim defined as\r\n{0}\r\n",
                    JObject.FromObject(new { claim.ClaimReference, ReserveTransactionHeaders = latestReservesHeaders }));
                var claimTransactionHeaders = claimHeader
                    .HistoricalClaimTransactionHeaders
                    .Where(cth => latestReservesHeaderIds.Contains(cth.ClaimTransactionHeaderID))
                    .OrderBy(cth => cth.TransactionDate)
                    .ToArray();

                foreach (var claimTransactionHeader in claimTransactionHeaders)
                {
                    try
                    {
                        if (claimTransactionHeader != null)
                        {
                            ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.FinancialTransaction,
                                                               claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode,
                                                               claimTransactionHeader.ClaimTransactionHeaderID);
                            Logger.InfoFormat(
                                "Attempting to create FINANCIALTRANSACTION transfer request\r\n{0}\r\n",
                                JObject.FromObject(
                                    new { claim.ClaimReference, claimTransactionHeader.ClaimTransactionHeaderID }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorFormat(
                            "Exception thrown while creating transfer request for Claim\r\n{0}\r\n",
                            JObject.FromObject(new { claimHeader.ClaimReference, claimTransactionHeader.ClaimTransactionHeaderReference, ex.Message, ex.StackTrace }));
                    }
                }
                // Submit a close Claim request too.  Then the normal Genius.X processing can reopen it.
                if (GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>().ReopeningClaims)
                {
                    ClaimTransfer.AddControlLogRequest((short)StaticValues.ClaimTransferType.Close,
                                                   claimHeader.ClaimReference, claimHeader.ClaimHeaderStatusCode, null);
                    Logger.InfoFormat("Attempting to create CLOSECLAIM trasfer requests\r\n{0}", JObject.FromObject(new { claim.ClaimReference}));
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(
                            "Exception thrown while creating transfer request for Claim\r\n{0}\r\n",
                            JObject.FromObject(new { claimHeader.ClaimReference, ex.Message, ex.StackTrace }));
            }
        }
    }
}
