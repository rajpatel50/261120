using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using log4net;
using System.Threading.Tasks;
using Xiap.DataMigration.GeniusInterface.AXACS.Gateways;
using Microsoft.Practices.Unity;
using log4net.Config;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ClaimMigratorOperations : IClaimMigratorOperations
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private TransferToGenius _migrator = null;
        private bool _isInBatchControllerStage; 
        private int _overallProcessed;
        private int _overallFailed;
        private int _overallTransferred;
        private int _overallDuplicated;
        private int _overallAlreadyProcessed;
        private int _skipped;
        private ClaimProcessorConfiguration _config;

        public ClaimMigratorOperations()
        {
            // Initialize Container
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "log4net.config")));

            //try
            //{
            //    var section = (UnityConfigurationSection)ConfigurationManager.GetSection("xiap/core/unity");
            //    section.Configure(GlobalClaimWakeUp.Container);
            //    section = (UnityConfigurationSection)ConfigurationManager.GetSection("xiap/custom/unity");
            //    section.Configure(GlobalClaimWakeUp.Container);
            //    GlobalClaimWakeUp.Container.RegisterType<TransferToGenius>();
            //}
            //catch (Exception ex)
            //{
            //    Logger.ErrorFormat("Problem initialising the Unity container! \n[\n\tMessage={0}\n\tStack={1}\n]", ex.Message, ex.StackTrace);
            //    return;
            //}
            XmlConfigurator.Configure();
            Logger.Info("== Starting up ClaimProcessor ==");

            try
            {
                //var config = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
                // Instantiate the object using constructor injection
                _config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
                _migrator = GlobalClaimWakeUp.Container.Resolve<TransferToGenius>();
                _migrator.SetConcurency(1);
                GlobalClaimWakeUp.Container.RegisterInstance<IGeniusXGateway>(new GeniusXGateway(_config.GeniusXConnectionString));
                // Change to allow for two different Staging DBs.
                GlobalClaimWakeUp.Container.RegisterType<Func<string, IStagingGateway>>(new InjectionFactory(c => new Func<string, IStagingGateway>((cr) =>
                {
                    if (cr.EndsWith("MO"))
                    {
                        return new StagingGateway(_config.StagingMotorConnectionString);
                    }
                    else
                    {
                        return new StagingGateway(_config.StagingLiabConnectionString);
                    }
                })));
                GlobalClaimWakeUp.Container.RegisterInstance<IGeniusGateway>(new GeniusGateway(_config.GeniusConnectionString));

            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Bogus! \n[\n\tMessage={0}\n\tStack={1}\n]", ex.Message, ex.StackTrace);
                throw;
            }

            Logger.Info("== Finished initialising ClaimProcessor ==");
        }


        public void ProcessClaim(string ClaimReference)
        {
            if (ClaimReference == null || ClaimReference.Trim().Length == 0)
            {
                Logger.Error("Call to ClaimWakeUpService Made with no claim reference provided!");
            }

            _skipped =
            _overallAlreadyProcessed =
            _overallDuplicated =
            _overallFailed =
            _overallProcessed =
            _overallTransferred = 0;

            _config.ClaimsReferencesToInclude = new string[1];
            _config.ClaimsReferencesToInclude[0] = ClaimReference;

            _isInBatchControllerStage = false;
            _config.ReopeningClaims = true;
            _config.StartingRecord = 0;

            Task.Factory.StartNew(StartMigration);
        }


        private void StartMigration()
        {
            _migrator.Transfer(_config);
        }

    }
}
