namespace Xiap.DataMigration.GeniusInterface.AXACS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks.Dataflow;
    using Entities;
    using log4net;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json.Linq;
    using Xiap.DataMigration.GeniusInterface.AXACS.Gateways;
    using Xiap.DataMigration.GeniusInterface.AXACS.Operations;
    using Xiap.Framework;
    using Xiap.Framework.Data.Underwriting;
    using Xiap.Framework.DataMapping;

	public delegate void GeniusXClaimProcessingEventHandler(object sender, UpdateEventArgs args);
    
	public class TransferToGenius : IDisposable
	{
		private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public event GeniusXClaimProcessingEventHandler OnUpdate;
        
		public event EventHandler OnComplete;

        private volatile int _concurrency = 8;

	    private static bool IsInitialized;
	    private int _totalClaims;

	    public void Transfer(ClaimProcessorConfiguration config)
        {
            if (!IsInitialized)
            {
                PropertyAccessorCache.InitializeCache();
                new UnderwritingService(XiapConstants.XIAP_DATASOURCE);
                MapperConfiguration.Initialize();
                IsInitialized = true;
            }
            
            Logger.Info("===============================================================================");
            Logger.Info("======================Starting Claim Processing================================");
            Logger.Info("===============================================================================");
	        
            try
            {
                if (config.FilterByPolicyReference)
                {
                    _totalClaims = GlobalClaimWakeUp.Container.Resolve<Func<string, IStagingGateway>>()(config.PolicyReferencesToInclude.First()).GetClaimByPolicyCount(
                        config.ProcessOpenClaims,
                        config.ProcessClosedClaims,
                        config.ProcessLiabilityClaims,
                        config.ProcessMotorClaims,
                        config.PolicyReferencesToInclude,
                        config.PolicyReferencesToExclude);
                }
                else
                {
                    _totalClaims = GlobalClaimWakeUp.Container.Resolve<Func<string, IStagingGateway>>()(config.ClaimsReferencesToInclude.First()).GetClaimByClaimCount(
                        config.ProcessOpenClaims,
                        config.ProcessClosedClaims,
                        config.ProcessLiabilityClaims,
                        config.ProcessMotorClaims,
                        config.ClaimsReferencesToInclude,
                        config.ClaimsReferencesToExclude);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Exception processing");
                Logger.Error(ex);
                throw ex;
            }

            Logger.InfoFormat("=======================Total Claims={0,-8}===================================", _totalClaims);
            Logger.InfoFormat("=======================Concurrency ={0,-8}===================================", _concurrency);
            if (OnUpdate != null) OnUpdate(this, new UpdateEventArgs { TotalClaims = _totalClaims, BatchSize = config.BatchSize });
            
            var index = config.StartingRecord;
            var page = Math.Max(config.BatchSize / 10, 10);
	        var postClaims = new ActionBlock<IEnumerable<Claim>>(chunk => PostClaimsToPipeline(chunk), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _concurrency });
            
            // ** Here for the validation that we only have Motor or only have liability claims/policies listed **

            do
            {
                IEnumerable<Claim> chunk;
                if (config.FilterByPolicyReference)
                {
                    chunk = GlobalClaimWakeUp.Container.Resolve<Func<string, IStagingGateway>>()(config.PolicyReferencesToInclude.First()).GetClaimsByPolicy(
                        page,
                        index,
                        config.ProcessOpenClaims,
                        config.ProcessClosedClaims,
                        config.ProcessLiabilityClaims,
                        config.ProcessMotorClaims,
                        config.PolicyReferencesToInclude,
                        config.PolicyReferencesToExclude);
                }
                else
                {
                    chunk = GlobalClaimWakeUp.Container.Resolve<Func<string, IStagingGateway>>()(config.ClaimsReferencesToInclude.First()).GetClaimsByClaim(
                        page,
                        index,
                        config.ProcessOpenClaims,
                        config.ProcessClosedClaims,
                        config.ProcessLiabilityClaims,
                        config.ProcessMotorClaims,
                        config.ClaimsReferencesToInclude,
                        config.ClaimsReferencesToExclude);
                }
                if (!chunk.Any()) break;
                postClaims.Post(chunk);
                index += page;
            } while (ShouldContinueProcessing(config, _totalClaims, index));

            postClaims.Complete();
            
            try
            {
                postClaims.Completion.Wait();
            }
            catch (AggregateException ex)
            {
                ex.Handle(e =>
                              {
                                  Logger.Error("Unhandled Exception!", e);
                                  return true;
                              });
            }

            if (OnComplete != null) OnComplete(this, new EventArgs());
        }

        private bool ShouldContinueProcessing(ClaimProcessorConfiguration config, int totalClaims, int index)
        {
            var shouldContinue = config.AutoSubmit ? index < totalClaims : index < config.BatchSize;

            Logger.InfoFormat("{0}", JObject.FromObject(new { config.AutoSubmit, config.BatchSize, totalClaims, index, shouldContinue }));
            return shouldContinue;
        }

        public void SetConcurency(int concurrency)
        {
            _concurrency = concurrency;
        }

        protected virtual void PostClaimsToPipeline(IEnumerable<Claim> chunk)
        {
            var claimMigratorOperations = GlobalClaimWakeUp.Container.Resolve<ClaimMigratorOperations>();
            claimMigratorOperations.OnUpdate +=
            (s, a) =>
            {
                if (OnUpdate != null)
                {
                    OnUpdate(s, new UpdateEventArgs
                    {
                        TotalClaims = _totalClaims,
                        BatchSize = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>().BatchSize,
                        ClaimAlreadyProcessed = a.ClaimAlreadyProcessed,
                        ClaimFailed = a.ClaimFailed,
                        ClaimIsDuplicate = a.ClaimIsDuplicate,
                        ClaimProcessed = a.ClaimProcessed,
                        ClaimReferenceCounts = a.ClaimReferenceCounts,
                        ClaimTransferred = a.ClaimTransferred,
                        SkippedClaims = a.SkippedClaims
                    });
                }
            };

            var innerCount = 0;
            claimMigratorOperations.SetConcurrency(_concurrency);
            claimMigratorOperations.InitializeDataflow();
            foreach (var claim in chunk)
            {
                try
                {
                    Logger.InfoFormat("Posting Claim\r\n{{\r\n\t\"ClaimReference\":\"{0}\"\r\n}}",
                                        claim.ClaimReference);
                    claimMigratorOperations.Post(claim);
                    ++innerCount;
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(
                        "Unhandled exception for claim batch\r\n{0}\r\n",
                        JObject.FromObject(
                        new
                        {
                            Claims = chunk.Select(c => c.ClaimReference).ToArray(),
                            ex.Message,
                            ex.InnerException
                        }));
                }
            }

            claimMigratorOperations.Complete();

            claimMigratorOperations.Wait();

            Logger.InfoFormat("Current Counts:\r\n{0}",
                   JObject.FromObject(
                   new
                   {
                      Count = innerCount,
                   }));
        }

	    public void Dispose()
	    {
            
	    }
	}
}
