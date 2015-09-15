using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GeniusX.AXA.FrontendModules.Claims.Resources;
using GeniusX.AXA.FrontendModules.Search.View;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Unity;
using Xiap.Framework.Logging;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Events;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.Frontend.Infrastructure.Validation;
using XIAP.FrontendModules.Infrastructure.Search;

namespace GeniusX.AXA.FrontendModules.Search
{
    public class AXASearchModule : BaseModule, IModule
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string DEEPLINK_GLOBAL_ACTION = "GLOBAL";
		private readonly IUnityContainer _container;
        private readonly IEventAggregator _eventAggregator;



        public AXASearchModule(IUnityContainer container, IEventAggregator eventAggregator, NavigationManager navigationManager)
            :base(container, eventAggregator, navigationManager)
        {
            this._container = container;
            this._eventAggregator = eventAggregator;
        }

        #region IModule Members

        public override void Initialize()
        {
			if (_Logger.IsInfoEnabled)
			{
				_Logger.Info("Module started initialisation");
			}

            this.RegisterViewsAndServices();
            this.ConfigureDeeplinking();

			if (_Logger.IsInfoEnabled)
			{
				_Logger.Info("Module completed initialisation");
			}

            this._eventAggregator.GetEvent<StartUpCompleteEvent>().Publish("AXASearchModule");
        }

        #endregion

        private void RegisterViewsAndServices()
        {
            this._container.RegisterType<UserControl, AXAClaimDataTemplate>("AXAClaimTemplate");
            this._container.RegisterType<UserControl, AXARiskDataTemplate>("AXARiskTemplate");
            var searchStringObject = Activator.CreateInstance(typeof(StringResources));
            this._container.RegisterInstance<object>("AXASearch", searchStringObject);
        }

        protected override void RegisterDeepLinkingLinks(DeeplinkingConfig deeplinkingConfig)
        {
        }
    }
}
