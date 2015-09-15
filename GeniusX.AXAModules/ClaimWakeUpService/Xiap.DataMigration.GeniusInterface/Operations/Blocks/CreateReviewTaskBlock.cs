namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Claims.BusinessComponent;
    using Entities;
    using Framework;
    using Framework.ProcessHandling;
    using Gateways;
    using GeniusX.AXA.Claims.BusinessLogic;
    using Metadata.Data.Enums;
    using Newtonsoft.Json.Linq;
    using log4net;
    using Microsoft.Practices.Unity;

    [BlockDescription("Create K2 Review Claim Tasks", Description = "")]
    public class CreateReviewTaskBlock : IBlock
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CreateReviewTaskBlock));

        private static Lazy<IEnumerable<ProductEvent>> _productEvents = new Lazy<IEnumerable<ProductEvent>>(() => GlobalClaimWakeUp.Container.Resolve<IGeniusXGateway>().GetProductEventIDs().ToArray());
        private static Lazy<ProductEvent> _motorProductEvents = new Lazy<ProductEvent>(() => _productEvents.Value.SingleOrDefault(pe => pe.ProductCode == GlobalClaimWakeUp.MotorProductCode.Value && pe.EventTypeCode == GlobalClaimWakeUp.ReviewEventTypeCode.Value));
        private static Lazy<ProductEvent> _liabilityProductEvents = new Lazy<ProductEvent>(() => _productEvents.Value.SingleOrDefault(pe => pe.ProductCode == GlobalClaimWakeUp.LiabilityProductCode.Value && pe.EventTypeCode == GlobalClaimWakeUp.ReviewEventTypeCode.Value));


        public static Claim Execute(Claim claim)
        {
            var sw = Stopwatch.StartNew();
            if (!ShouldExecute(claim)) return claim;
            var transaction = claim.CreateAmendClaimWithoutValidationTransaction();
            if (transaction == null) return claim;
            try
            {
                var claimHeader = transaction.ClaimHeader;

                if (claimHeader.ClaimHeaderInternalStatus >= (short)StaticValues.ClaimHeaderInternalStatus.Finalized) return claim;
                var productEvent = claim.ProductType == "MOT" ? _motorProductEvents.Value : _liabilityProductEvents.Value;

                var reviewEvent = CreateReviewEvent(claimHeader, claim.GeniusXClaimHandler, productEvent.ProductEventId);
                transaction.Complete(false);
                if (reviewEvent != null)
                {
                    var processHandler = GlobalClaimWakeUp.Container.Resolve<ClaimsProcessHandler>();
                    processHandler.ProcessComponent(reviewEvent, ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Alias = "AXAManualReviewStartClaimProcessHandler" });
                    Logger.InfoFormat("Review Task successfully created for Claim\r\n{0}\r\n", JObject.FromObject(new { claimHeader.ClaimReference, UserId = claim.GeniusXClaimHandler }));
                }
                return claim;
            }
            catch
            {
                transaction.Cancel();
                return claim;
            }
            finally
            {
                sw.Stop();
                var workDone = GlobalClaimWakeUp.Statistics.GetOrAdd(typeof(CreateReviewTaskBlock).Name, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                GlobalClaimWakeUp.Statistics[typeof(CreateReviewTaskBlock).Name] = (workDone + TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
        }

        private static bool ShouldExecute(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            return config.TaskIsEnabled[typeof(CreateReviewTaskBlock)] && !config.ReopeningClaims && 
             (!claim.ClaimIsDuplicate && !claim.ClaimIsClosed && !claim.ClaimProcessingFailed && !claim.ClaimAlreadyProcessed);
        }

        private static ClaimEvent CreateReviewEvent(ClaimHeader claimHeader, long claimHandlerId, long productEventID)
        {
            try
            {
                var reviewEvent = claimHeader.AddNewClaimEvent(productEventID);
                reviewEvent.CreatedByUserID = claimHandlerId;
                reviewEvent.TaskInitialUserID = claimHandlerId;
                reviewEvent.CreatedDate = DateTime.Now;
                reviewEvent.TaskInitialDueDate = DateTime.Now;
                reviewEvent.EventDescription = "Migrated Claim Review";
                reviewEvent.TaskInitialDetails = "Migrated Claim Review";

                return reviewEvent;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Exception thrown while creating review task for Claim\r\n{0}\r\n",
                    JObject.FromObject(new { claimHeader.ClaimReference, ex.Message, ex.StackTrace }));
                return null;
            }
        }
    }
}
