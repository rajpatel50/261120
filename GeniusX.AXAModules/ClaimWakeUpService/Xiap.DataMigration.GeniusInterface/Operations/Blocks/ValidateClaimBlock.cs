using System.Globalization;
using Xiap.Claims.BusinessTransaction;
using Xiap.Framework.Caching;
using Xiap.Framework.Locking;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Entities;
    using Framework;
    using Framework.ProcessHandling;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json.Linq;
    using log4net;
    using log4net.Repository.Hierarchy;

    [BlockDescription("Validate Claims", Description = "")]
    public class ValidateClaimBlock : IBlock
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ValidateClaimBlock));

        public static Claim Execute(Claim claim)
        {
            var sw = Stopwatch.StartNew();

            if (!ShouldExecute(claim)) return claim;
            ClearLocksBlock.UnlockClaim(claim);

            try
            {
                if (!Reopening(claim))
                {
                    var transaction = claim.CreateAmendClaimTransaction();
                    if (transaction == null) return claim;
                    ValidateClaimUsingTransactionRules(transaction, claim);
                    transaction.Cancel();
                }
                if (string.IsNullOrEmpty(claim.CustomCode18)) claim.CustomCode18 = claim.ExcessAndDeductiblesToProcess ? "C01" : "C02";
                return claim;
            }
            finally
            {
                sw.Stop();
                var workDone = GlobalClaimWakeUp.Statistics.GetOrAdd(typeof(ValidateClaimBlock).Name, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                GlobalClaimWakeUp.Statistics[typeof(ValidateClaimBlock).Name] = (workDone + TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
        }

        private static bool ShouldExecute(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            return config.TaskIsEnabled[typeof(ValidateClaimBlock)] && 
             (!claim.ClaimIsDuplicate && !claim.ClaimAlreadyProcessed && !claim.ClaimProcessingFailed);
        }

        private static bool Reopening(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            return config.ReopeningClaims;
        }

        private static void ValidateClaimUsingTransactionRules(AbstractClaimsBusinessTransaction amendTransaction, Claim claim)
        {
            // The System Rules Plugin is run for any component change - need to verify it will be run across components that haven't changed as well.
            var claimHeader = amendTransaction.ClaimHeader;
            try
            {
                amendTransaction.Validate(ValidationMode.Full);
                var errorOrFatalErrorMessages =
                    amendTransaction.Results.SelectMany(
                        r =>
                        r.Value.SelectMany(
                            kvp =>
                            kvp.Value.Where(
                                pr => (pr.Severity == ErrorSeverity.Error || pr.Severity == ErrorSeverity.Fatal)
                            )
                        )
                    )
                    .ToArray();
                if (errorOrFatalErrorMessages.Any())
                {
                    Logger.WarnFormat("Claim failed validation\r\n{0}\r\n", JObject.FromObject(
                        new
                        {
                            claimHeader.ClaimReference,
                            Messages = errorOrFatalErrorMessages.Select(m => new { m.Severity, m.MessageSource, m.Message }).ToArray()
                        }));
                    claim.CustomCode18 = "V02";
                }
            }
            catch (Exception exc)
            {
                Logger.WarnFormat("Fatal validation error for Claim \r\n{0}\r\n", JObject.FromObject(new { amendTransaction.ClaimHeader.ClaimReference, exc.Message, exc.StackTrace }));
                claim.CustomCode18 = "V01";
            }
        }
    }
}
