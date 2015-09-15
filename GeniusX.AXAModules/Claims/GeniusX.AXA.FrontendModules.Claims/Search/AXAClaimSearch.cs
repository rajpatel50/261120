using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Claims.Search.Controller;

namespace GeniusX.AXA.FrontendModules.Claims.Search
{
    /// <summary>
    /// Search provider class.
    /// </summary>
    public class AXAClaimSearch : AXAClaimSearchBase
    {
        private const string CLAIMS_SEARCH_TYPE = "ClaimSummaryAmountsSearchProvider";
        public AXAClaimSearch(ClaimPreviewController previewController, ISearchServiceHandler searchService, IUnityContainer container, AppModel appModel)
            : base(previewController, searchService, container, appModel)
        {
            this._searchTypeName = CLAIMS_SEARCH_TYPE;
        }
    }
}
