namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Entities;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json.Linq;
    using log4net;

    [BlockDescription("Set Claim Process status", Description = "")]
    public class SetCustomCode18Block : IBlock
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExcessAndDeductiblesBlock));

        public static void Execute(Claim claim)
        {
            var sw = Stopwatch.StartNew();
            var transaction = claim.CreateAmendClaimWithoutValidationTransaction();
            if (transaction == null) return;
            try
            {
                List<FlattenedTransaction> transactions;
                GlobalClaimWakeUp.MappedTransactionDetails.TryRemove(claim.ClaimReference, out transactions);
                if (ShouldExecute(claim))
                {
                    
                    var claimHeader = transaction.ClaimHeader;
                    claimHeader.CustomCode18 = claim.CustomCode18;
                    transaction.Complete(false);
                }
            }
            catch
            {
                transaction.Cancel();
            }
            finally
            {
                sw.Stop();
                var workDone = GlobalClaimWakeUp.Statistics.GetOrAdd(typeof(SetCustomCode18Block).Name, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                GlobalClaimWakeUp.Statistics[typeof(SetCustomCode18Block).Name] = (workDone + TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
        }

        private static bool ShouldExecute(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            return config.TaskIsEnabled[typeof (SetCustomCode18Block)] && !claim.ClaimAlreadyProcessed;
        }
    }
}
