namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks.Dataflow;
    using Blocks;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json.Linq;
    using Xiap.DataMigration.GeniusInterface.AXACS.Entities;
    using log4net;

    internal class ClaimMigratorOperations
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private int _totalClaims;

        private int _skippedClaims;

        private TransformBlock<Claim, Claim> _attachClaimToPolicy;

        private TransformBlock<Claim, Claim> _executeExcessAndDeductibles;

        private TransformBlock<Claim, Claim> _validateClaim;

        private TransformBlock<Claim, Claim> _createTransferClaimRequest;

        private TransformBlock<Claim, Claim> _createTransferNameRequests;

        private TransformBlock<Claim, Claim> _createClaimReviewTask;

        private TransformBlock<Claim, Claim> _clearLocks;

        private TransformBlock<Claim, Claim> _setCustomCode18;

        private ActionBlock<Claim> _logClaimState;

        private ClaimProcessorConfiguration _config;

        private volatile int _concurrency = 8;

        public event GeniusXClaimProcessingEventHandler OnUpdate;

        public void Post(Claim claim)
        {
            List<Claim> postedClaims;
            if (!GlobalClaimWakeUp.PostedClaims.TryGetValue(GetHashCode(), out postedClaims))
            {
                postedClaims = new List<Claim>();
                GlobalClaimWakeUp.PostedClaims.TryAdd(GetHashCode(), postedClaims);
            }
            if (postedClaims.FirstOrDefault(c => string.Equals(c.ClaimReference, claim.ClaimReference, StringComparison.OrdinalIgnoreCase)) == null)
            {
                postedClaims.Add(claim);
                _attachClaimToPolicy.Post(claim);
            }
            else
            {
                claim.ClaimProcessingCompleted = true;
                claim.ClaimIsDuplicate = true;
                Logger.WarnFormat("Possible duplicate Claim encountered\r\n[\r\n\tClaimReference={0}\r\n]", claim.ClaimReference);
                _attachClaimToPolicy.Post(claim);
            }
        }

        public void Complete()
        {
            _attachClaimToPolicy.Complete();
        }

        public void Wait()
        {
            if (_logClaimState.Completion.IsCompleted) return;
            try
            {
                _logClaimState
                    .Completion
                     .Wait();
            }
            catch (AggregateException ex)
            {
                ex.Handle(
                    e =>
                        {
                            Logger.ErrorFormat(
                                "Unhandled exception for claim batch\r\n{0}",
                                JObject.FromObject(new
                                                       {
                                                           e.Message,
                                                           e.StackTrace
                                                       })
                                );
                            if (e.InnerException != null && string.IsNullOrEmpty(e.StackTrace))
                            {
                                var ie = e.InnerException;
                                while (ie != null)
                                {
                                    Logger.ErrorFormat(
                                        "Unhandled exception for claim batch\r\n{0}",
                                        JObject.FromObject(new
                                                               {
                                                                   ie.Message,
                                                                   ie.StackTrace
                                                               })
                                        );
                                    ie = ie.InnerException;
                                }
                            }

                            List<Claim> postedClaims;
                            if (GlobalClaimWakeUp.PostedClaims.TryRemove(GetHashCode(), out postedClaims))
                            {
                                _skippedClaims = postedClaims.Count(c =>
                                                                    !c.ClaimAlreadyProcessed &&
                                                                    !c.ClaimIsAttachedToPolicy &&
                                                                    !c.ClaimIsDuplicate &&
                                                                    !c.ClaimProcessingCompleted &&
                                                                    !c.ClaimProcessingFailed &&
                                                                    !c.ClaimTransfered);
                                Logger.WarnFormat("Skipping\r\n{0}", JObject.FromObject(
                                    new
                                        {
                                            SkippedClaims = postedClaims.Select(c => c.ClaimReference).ToArray()
                                        }));

                                postedClaims.Clear();
                            }
                            if (OnUpdate != null)
                            {
                                OnUpdate(this, new UpdateEventArgs
                                                   {
                                                       SkippedClaims = _skippedClaims
                                                   });
                            }
                            return true;
                        });
            }

            Logger.Info("===============================================================================");
            Logger.Info("======================Completed Claim Processing===============================");
            Logger.InfoFormat("Processing stats:\r\n{0}", JObject.FromObject(new {GlobalClaimWakeUp.Statistics}));
            Logger.Info("===============================================================================");
        }

        public void InitializeDataflow()
        {
            _config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            Logger.Info("======================Initializing Dataflow=================================");
            
            GlobalClaimWakeUp.PostedClaims.Clear();
            if (_config.ReopeningClaims)
            {
                GlobalClaimWakeUp.GeniusXPolicyState.Clear();
                GlobalClaimWakeUp.NameReferences.Clear();
                GlobalClaimWakeUp.PostedClaims.Clear();
            }
            

            _attachClaimToPolicy = new TransformBlock<Claim, Claim>(
                c => AttachClaimToPolicyBlock.Execute(c),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });


            _executeExcessAndDeductibles = new TransformBlock<Claim, Claim>(
                    c => ExcessAndDeductiblesBlock.Execute(c),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            _createTransferNameRequests = new TransformBlock<Claim, Claim>(
                    c => CreateNameTransferRequestsBlock.Execute(c),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            // We don't need to create a review task for the service
            //_createClaimReviewTask = new TransformBlock<Claim, Claim>(
            //        c => CreateReviewTaskBlock.Execute(c),
            //        new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            _createTransferClaimRequest = new TransformBlock<Claim, Claim>(
                    c => CreateClaimTransferRequestsBlock.Execute(c),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            _clearLocks = new TransformBlock<Claim, Claim>(
                c => ClearLocksBlock.Execute(c),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            _validateClaim = new TransformBlock<Claim, Claim>(
                    c => ValidateClaimBlock.Execute(c),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            _setCustomCode18 = new TransformBlock<Claim, Claim>(
                    c =>
                        {
                            SetCustomCode18Block.Execute(c);
                            UpdateMigrationStatus(c);
                            return c;
                        },
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 });

            _logClaimState = new ActionBlock<Claim>(
                c =>
                    {
                        Logger.InfoFormat("Claim status is\r\n{0}", JObject.FromObject(c));
                    });
            
            //_attachClaimToPolicy.LinkTo(_createClaimReviewTask, new DataflowLinkOptions { PropagateCompletion = true });
            //_createClaimReviewTask.LinkTo(_executeExcessAndDeductibles, new DataflowLinkOptions { PropagateCompletion = true });
            // Created next line to avoid the review task creation.
            _attachClaimToPolicy.LinkTo(_executeExcessAndDeductibles, new DataflowLinkOptions { PropagateCompletion = true });
            _executeExcessAndDeductibles.LinkTo(_createTransferClaimRequest, new DataflowLinkOptions { PropagateCompletion = true });
            _createTransferClaimRequest.LinkTo(_createTransferNameRequests, new DataflowLinkOptions { PropagateCompletion = true });
            _createTransferNameRequests.LinkTo(_validateClaim, new DataflowLinkOptions { PropagateCompletion = true });
            _validateClaim.LinkTo(_clearLocks, new DataflowLinkOptions { PropagateCompletion = true });
            _clearLocks.LinkTo(_setCustomCode18, new DataflowLinkOptions { PropagateCompletion = true });
            _setCustomCode18.LinkTo(_logClaimState, new DataflowLinkOptions {PropagateCompletion = true});
        }

        public void SetConcurrency(int concurrency)
        {
            _concurrency = concurrency;
        }

        private void UpdateMigrationStatus(Claim c)
        {
            if (OnUpdate != null)
            {
                var args = new UpdateEventArgs
                               {
                                   TotalClaims = _totalClaims,
                                   ClaimFailed = c.ClaimProcessingFailed,
                                   BatchSize = _config.BatchSize,
                                   ClaimProcessed = c.ClaimProcessingCompleted,
                                   ClaimAlreadyProcessed = c.ClaimAlreadyProcessed,
                                   ClaimTransferred = c.ClaimTransfered,
                                   ClaimIsDuplicate = c.ClaimIsDuplicate
                               };
                Logger.InfoFormat("Claim marked as\r\n{0}\r\n", JObject.FromObject(new {c.ClaimReference, EventArgs=args}));
                OnUpdate(this, args);
            }
        }
    }
}
