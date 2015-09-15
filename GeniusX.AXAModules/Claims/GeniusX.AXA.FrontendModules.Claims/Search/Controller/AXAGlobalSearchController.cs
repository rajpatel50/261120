using GeniusX.AXA.FrontendModules.Claims.Search.Model;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using XIAP.FrontendModules.Search.Controller;

namespace GeniusX.AXA.FrontendModules.Claims.Search.Controller
{
    public class AXAGlobalSearchController : GlobalSearchController
    {
        public AXAGlobalSearchController(IUnityContainer container, IEventAggregator eventAggregator, AXASearchResultsModel model)
            : base(eventAggregator, model, container)
        {
        }
    }
}
