using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Claims.Search.Controller;
using XIAP.FrontendModules.Common.SearchService;

namespace GeniusX.AXA.FrontendModules.Claims.Search
{
    /// <summary>
    /// This class will perform the search functionality and will bind the results.
    /// </summary>
    public abstract class AXAClaimSearchBase : IDisposable
    {
        private const string CLAIMS_SEARCH_SP_NAME = "[Claims].[DuplicateClaimCheck]";
        private const string DEFAULT_SORT_COLUMN = "ClaimReference";
        private ISearchServiceHandler _searchService;
        private IUnityContainer _container;
        protected string _searchTypeName;
        private AppModel _appModel;
        private SearchParameters _searchParameters;
        private ClaimPreviewControllerBase _previewController;

        public AXAClaimSearchBase(ClaimPreviewControllerBase previewController, ISearchServiceHandler searchService, IUnityContainer container, AppModel appModel)
        {
            this._searchService = searchService;
            this._container = container;
            this._appModel = appModel;
            this._previewController = previewController;
        }

        public void RegisterSearchChannel(SearchContext searchContext)
        {
            this._searchService.GetSearchParameters(CLAIMS_SEARCH_SP_NAME, parameters =>
            {
                this._searchParameters = parameters;
                searchContext.RegisterSearchChannel(this.GetChannel(this._searchTypeName));
            });
        }

        public ISearchChannel GetChannel(string searchTypeName)
        {
            SearchChannel searchChannel = new SearchChannel(this.LoadPreview, this.GetResults, null, this.Dispose);
            searchChannel.ProviderName = searchTypeName;

            return searchChannel;
        }

        protected virtual SearchCriteria BuildSearchCriteria(SearchRequest searchRequest)
        {
            System.Collections.Generic.Dictionary<string, bool> claimsSortings = new Dictionary<string, bool>() { { "ClaimReference", true } };

            SearchCriteria searchCriteria = SearchHelper.BuildDefaultSearchCriteria(searchRequest, DEFAULT_SORT_COLUMN, SortOrder.Ascending, null);
            //// For Default sorting order if sorting is column is not given.
            if (string.IsNullOrEmpty(searchRequest.SortColumn))
            {
                searchCriteria.SortOrderList = claimsSortings;
            }

            ObservableCollection<SearchCriteriaValue> criteriaValues = this._searchParameters.GenerateSearchCriteria();
            Dictionary<string, SearchCriteriaValue> criteriaLookup = criteriaValues.ToDictionary(p => p.Name, p => p);

            var claimsSearchCriteria = searchRequest.Filters;
            if (claimsSearchCriteria == null)
            {
                throw new ArgumentException("ClaimsSearchCriteria");
            }

            criteriaLookup["DclDateOfLoss"].Value = claimsSearchCriteria.Where(a => a.Key.Contains("DclDateOfLoss")).Single().Value;
            criteriaLookup["ClientID"].Value = claimsSearchCriteria.Where(a => a.Key.Contains("ClientID")).Single().Value;
            criteriaLookup["ClaimantSurname"].Value = claimsSearchCriteria.Where(a => a.Key.Contains("ClaimantSurname")).Single().Value;
            criteriaLookup["ClientReference"].Value = claimsSearchCriteria.Where(a => a.Key.Contains("ClientReference")).Single().Value;
            criteriaLookup["OutsourceReference"].Value = claimsSearchCriteria.Where(a => a.Key.Contains("OutsourceReference")).Single().Value;
            criteriaLookup["DclProductCode"].Value = claimsSearchCriteria.Where(a => a.Key.Contains("DclProductCode")).Single().Value;

            string productCode = claimsSearchCriteria.Where(a => a.Key.Contains("DclProductCode")).Single().Value as string;

            if (productCode != AXAClaimConstants.LIABILITY_CLAIM_PRODUCT)
            {
                criteriaLookup["DriverSurname"].Value = claimsSearchCriteria.Where(a => a.Key.Contains("DriverSurname")).Single().Value;
                criteriaLookup["DclRegistrationNbr"].Value = claimsSearchCriteria.Where(a => a.Key.Contains("DclRegistrationNbr")).Single().Value;
            }
               
            searchCriteria.ValueList = criteriaValues;
            return searchCriteria;
        }

        ////This method will invoke the search and add the resuls to the ClaimSearchRow collection binded to the view.
        public virtual void GetResults(SearchRequest request, ResultsCallback callback)
        {
            this._searchService.InvokeSearch(this._appModel.UserProfile.Culture,
            this._appModel.UserProfile.LanguageId,
            CLAIMS_SEARCH_SP_NAME,
            this.BuildSearchCriteria(request),
            (searchData) =>
            {
                ObservableCollection<ISearchRow> nameRows = searchData.SearchResultRowList.Transform<SearchResultRow, ISearchRow>((row) =>
                {
                    GeniusX.AXA.FrontendModules.Claims.Search.Model.AXAClaimSearchRow riskRow = new GeniusX.AXA.FrontendModules.Claims.Search.Model.AXAClaimSearchRow(row);
                    riskRow.ViewUniqueId = this._searchTypeName + "Preview";
                    return riskRow;
                });

                callback(new DefaultSearchResult(request.RecordIndex, searchData.TotalRecords, nameRows));
            });
        }

        ////Loads the preview of the selected claim, similar to the core XIAP functionality.
        public void LoadPreview(IEnumerable<ISearchRow> rows, ViewCallback callback, HandleErrorCallback handleErrorCallback)
        {
            this._previewController.LoadPreview(rows, arg => callback(arg), handleErrorCallback);
        }

        public void Dispose()
        {
        }
    }
}
