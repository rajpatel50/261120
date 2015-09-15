using GeniusX.AXA.FrontendModules.InsuranceDirectory.Controller;
using GeniusX.AXA.FrontendModules.InsuranceDirectory.Navigation;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Unity;
using Xiap.Framework.Logging;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Events;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Application;

namespace GeniusX.AXA.FrontendModules.InsuranceDirectory
{
    public class AXAInsuranceDirectoryModule : BaseModule, IModule
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private IUnityContainer _container;
        private IEventAggregator _eventAggregator;

        public AXAInsuranceDirectoryModule(IUnityContainer container, IEventAggregator eventAggregator, NavigationManager navigationManager)
            : base(container, eventAggregator, navigationManager)
        {
            this._container = container;
            this._eventAggregator = eventAggregator;
        }

        public new void Initialize()
        {
            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info("AXA InsuranceDirectory Module started initialisation");
            }

            this.RegisterViewsAndServices();

            this._eventAggregator.GetEvent<StartUpCompleteEvent>().Publish("AXAInsuranceDirectory");

            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info("AXA InsuranceDirectory Module completed initialisation");
            }

            base.Initialize();
        }

        protected void RegisterViewsAndServices()
        {
            this._container.RegisterType<IViewController, AXACompanyController>("AXACompanyController");
            this._container.RegisterType<IViewController, AXAPersonController>("AXAPersonController");
            this._container.RegisterType<INodeAvailabilityBehaviour, AXAFinancialAccountsAvailableBehaviour>("IsFinancialAccountsAvailable");
            return;
        }

        protected override void RegisterDeepLinkingLinks(DeeplinkingConfig deeplinkingConfig)
        {
        }
    }
}
