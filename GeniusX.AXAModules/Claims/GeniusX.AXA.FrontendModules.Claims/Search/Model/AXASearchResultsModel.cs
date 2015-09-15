using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Model;
using GeniusX.AXA.FrontendModules.Claims.Resources;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Configuration;
using XIAP.Frontend.Infrastructure.Utils;
using XIAP.FrontendModules.Infrastructure.Search;
using XIAP.FrontendModules.Search.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Search.Model
{
    public class AXASearchResultsModel : SearchResultsModel
    {
        private AppModel _appModel;
        public AXASearchResultsModel(IDelayedEvent<SelectedRowChangedEventArgs> timer, AppModel appModel)
            : base(timer, appModel)
        {
            this._appModel = appModel;
        }

        protected override SearchResultModel CreateSearchResult(string searchGroup, string providerName, string header)
        {
            if (providerName == StringResources.SearchResultsModel_Claims)
            {
                AXASearchResultModel result = new AXASearchResultModel();
                result.ProviderName = providerName;
                result.HeaderText = header;
                result.SearchGroup = searchGroup;
                return result;
            }

            return base.CreateSearchResult(searchGroup, providerName, header);
        }
    }
}
