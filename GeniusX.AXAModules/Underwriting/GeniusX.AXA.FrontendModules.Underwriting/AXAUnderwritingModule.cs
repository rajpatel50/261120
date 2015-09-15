using GeniusX.AXA.FrontendModules.Underwriting.Controller;
using GeniusX.AXA.FrontendModules.Underwriting.Controller.Search;
using GeniusX.AXA.FrontendModules.Underwriting.Views.Header;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Xiap.Framework.Logging;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Events;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.FrontendModules.Application;

namespace GeniusX.AXA.FrontendModules.Underwriting
{
    public class AXAUnderwritingModule : BaseModule, IModule
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ApplicationModel _application;
        private IUnityContainer _container;
        private IRegionManager _regionManager;
        private IEventAggregator _eventAggregator;
        private NavigationManager _navigationManager;

        public AXAUnderwritingModule(ApplicationModel application, IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator, NavigationManager navigationManager)
            : base(container, eventAggregator,navigationManager)
        {
            this._application = application;
            this._container = container;
            this._regionManager = regionManager;
            this._eventAggregator = eventAggregator;
            this._navigationManager = navigationManager;
        }

        public new void Initialize()
        {
            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info("AXA Underwriting Module started initialisation");
            }

            this.RegisterViewsAndServices();

            this._eventAggregator.GetEvent<StartUpCompleteEvent>().Publish("AXAUnderwriting");

            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info("AXA Underwriting Module completed initialisation");
            }

            base.Initialize();
        }


        protected void RegisterViewsAndServices()
        {
            this._container.RegisterType<IViewController, AXAPolicyRiskController>("AXAPolicyRiskController");
            this._container.RegisterType<IView, MainDetailsPanel>("AXAMainDetailPanel");
            this._container.RegisterType<ISearchPopupController, AXAPolicySearchController>("AXAPolicySearchController");
            this._container.RegisterType<ISearchPopupController, AXACoverageVerificationPolicySearchController>("AXACoverageVerificationPolicySearchController");
            return;
        }

        protected override void RegisterDeepLinkingLinks(DeeplinkingConfig deeplinkingConfig)
        {
            throw new System.NotImplementedException();
        }
    }
}
