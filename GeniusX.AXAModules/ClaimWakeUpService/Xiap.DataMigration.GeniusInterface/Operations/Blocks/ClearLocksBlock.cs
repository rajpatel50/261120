using System.Globalization;
using Xiap.Framework.BusinessTransaction;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Entities;
    using Framework.Locking;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json.Linq;
    using log4net;

    [BlockDescription("Clear Locks", Description = "Clears the locks from the Claim")]
    public class ClearLocksBlock : IBlock
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ClearLocksBlock));

        public static Claim Execute(Claim claim)
        {
            if (!ShouldExecute(claim)) return claim;
            return UnlockClaim(claim);
        }

        public static Claim UnlockClaim(Claim claim)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var parameters = new[]
                                     {
                                         claim.GeniusXHeaderId.ToString(CultureInfo.InvariantCulture), true.ToString(),
                                         String.Empty, false.ToString()
                                     };
                var transaction =
                    (AmendClaimWithoutValidationTransaction)
                    BusinessTransactionFactory.GetBusinessTransactionByName("Claims.AmendClaimWithoutValidation", parameters);
                var claimHeaderId = transaction.ClaimHeader.ClaimHeaderID;
                var policyHeaderID = transaction.ClaimHeader.PolicyHeaderID;
                transaction.Cancel();
                var claimHeaderLocks = LockManager.CheckLock(claimHeaderId.ToString(CultureInfo.InvariantCulture),
                                                             LockLevel.ClaimHeader,
                                                             LockDurationType.Persistent, LockType.Update,
                                                             LockOrigin.Underwriting);
                if (claimHeaderLocks.Any())
                {
                    foreach (var @lock in claimHeaderLocks)
                    {
                        LockManager.RemoveLock(@lock.LockLevel, @lock.LockData, @lock.TransactionID);
                        Logger.InfoFormat("Clearing Claim Header lock\r\n{0}",
                                          JObject.FromObject(new {claim.ClaimReference, Lock = @lock}));
                    }
                }

                var claimReferenceLocks = LockManager.CheckLock(claim.ClaimReference, LockLevel.ClaimReference,
                                                                LockDurationType.Transaction, LockType.Update,
                                                                LockOrigin.ClaimInput);

                if (claimReferenceLocks.Any())
                {
                    foreach (var @lock in claimReferenceLocks)
                    {
                        LockManager.RemoveLock(@lock.LockLevel, @lock.LockData, @lock.TransactionID);
                        Logger.InfoFormat("Clearing Claim Reference lock\r\n{0}",
                                          JObject.FromObject(new {claim.ClaimReference, Lock = @lock}));
                    }
                }

                var policyReferenceLocks = LockManager.CheckLock(policyHeaderID.ToString(), LockLevel.HeaderReference,
                                                                 LockDurationType.Transaction, LockType.Update,
                                                                 LockOrigin.Underwriting);
                if (policyReferenceLocks.Any())
                {
                    foreach (var @lock in policyReferenceLocks)
                    {
                        LockManager.RemoveLock(@lock.LockLevel, @lock.LockData, @lock.TransactionID);
                        Logger.InfoFormat("Clearing Policy locks\r\n{0}",
                                          JObject.FromObject(new {claim.PolicyNumber, Lock = @lock}));
                    }
                }
                claim.ClaimProcessingCompleted = true;

                return claim;
            }
            finally
            {
                sw.Stop();
                var workDone = GlobalClaimWakeUp.Statistics.GetOrAdd(typeof (ClearLocksBlock).Name,
                                                          TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                GlobalClaimWakeUp.Statistics[typeof (ClearLocksBlock).Name] = (workDone + TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
        }

        private static bool ShouldExecute(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            return config.TaskIsEnabled[typeof(ClearLocksBlock)] && (!claim.ClaimIsDuplicate && !claim.ClaimProcessingFailed);
        }
    }
}
