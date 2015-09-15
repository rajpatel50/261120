namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Claims.BusinessComponent;
    using Entities;
    using Framework.Common;
    using Framework.Data.InsuranceDirectory;
    using Newtonsoft.Json.Linq;
    using log4net;
    using Microsoft.Practices.Unity;

    [BlockDescription("Create Name Transfer requests", Description = "")]
    public class CreateNameTransferRequestsBlock : IBlock
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CreateNameTransferRequestsBlock));

        public static Claim Execute(Claim claim)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (!ShouldExecute(claim)) return claim;

                var transaction = claim.CreateAmendClaimWithoutValidationTransaction();
                if (transaction.ClaimHeader == null)
                {
                    transaction.Cancel();
                    return claim;
                }
                CreateNameTransferRequests(transaction.ClaimHeader);
                transaction.Cancel();
                return claim;
            }
            finally
            {
                sw.Stop();
                var workDone = GlobalClaimWakeUp.Statistics.GetOrAdd(typeof(CreateNameTransferRequestsBlock).Name, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                GlobalClaimWakeUp.Statistics[typeof(CreateNameTransferRequestsBlock).Name] = (workDone + TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
        }

        private static bool ShouldExecute(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            if (config.ReopeningClaims)
                return config.TaskIsEnabled[typeof(CreateNameTransferRequestsBlock)] &&
                    (!claim.ClaimIsDuplicate && claim.ClaimIsClosed && !claim.ClaimProcessingFailed && !claim.ClaimAlreadyProcessed);

            return config.TaskIsEnabled[typeof(CreateNameTransferRequestsBlock)] &&
             (!claim.ClaimIsDuplicate && !claim.ClaimIsClosed && !claim.ClaimProcessingFailed && !claim.ClaimAlreadyProcessed);
        }

        private static void CreateNameTransferRequests(ClaimHeader claimHeader)
        {

            var idService = GlobalClaimWakeUp.Container.Resolve<IInsuranceDirectoryService>();
            foreach (var nameInvolvement in GetTransientNames(claimHeader))
            {
                try
                {
                    // NameInvolvement can only be CLAIMANT, DRIVER, WITNESS otherwise it should have been setup in Genius already
                    var nameData = idService.GetName(nameInvolvement.NameID.GetValueOrDefault(-1));
                    if (GlobalClaimWakeUp.NameReferences.Contains(nameData.NameReference))
                    {
                        Logger.DebugFormat("NAME already set to transfer\r\n{0}\r\n", JObject.FromObject(new { claimHeader.ClaimReference, nameData.NameReference }));
                        continue;
                    }
                    GlobalClaimWakeUp.NameReferences.Add(nameData.NameReference);
                    NameTransfer.AddControlLogRequest(nameData.NameReference, GlobalClaimWakeUp.ActingUserId.Value);
                    Logger.InfoFormat("NAME transfer created for\r\n{0}\r\n", JObject.FromObject(new { claimHeader.ClaimReference, nameData.NameReference }));
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(
                        "Exception thrown while transfering NAME\r\n{0}\r\n",
                        JObject.FromObject(new { claimHeader.ClaimReference, nameInvolvement.Name.NameReference, ex.Message, ex.StackTrace }));

                }
            }
        }

        private static IEnumerable<ClaimNameInvolvement> GetTransientNames(ClaimHeader claimHeader)
        {
            return claimHeader
                .NameInvolvements
                .Cast<ClaimNameInvolvement>()
                .Where(ni => GlobalClaimWakeUp.NameUsageTypeCodes.Contains(ni.NameUsageTypeCode));
        }
    }
}
