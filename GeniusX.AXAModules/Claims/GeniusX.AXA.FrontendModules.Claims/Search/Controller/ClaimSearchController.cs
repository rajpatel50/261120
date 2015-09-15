using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Search.Model;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Configuration;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Search;
using XIAP.FrontendModules.Search.Controller;
using XIAP.FrontendModules.Search.Manager;
using XIAP.FrontendModules.Search.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Search.Controller
{
    /// <summary>
    /// This class will work as global search controller for AXA, it will initiate the duplicate search.
    /// </summary>
    public class ClaimSearchController : GlobalSearchController
    {
        private const string ProviderName = "ClaimSummaryAmountsSearchProvider";
        private ApplicationModel _applicationModel;
        private IClaimClientService _claimsService;
        private IUnityContainer _claimContainer;
        private IMetadataClientService _metadataService;
        private IEventAggregator _eventAggregator;
        private SearchManager _searchManager;
        private SearchPreviewRegionHelper _previewRegionHelper;
        // change modifier from private to public of ClaimSearchModel to access it at view
        public ClaimSearchModel ClaimSearchModel;

        public ClaimSearchController(ApplicationModel applicationModel, IUnityContainer container, IClaimClientService claimService, AppModel appModel, ISearchServiceHandler searchService, SearchResultsModel searchFilterModel, IMetadataClientService metadataService, IEventAggregator eventAggregator, ClaimSearchModel searchModel)
            : base(eventAggregator, searchFilterModel, container)
        {
            this.ClaimSearchModel = searchModel;
            this._applicationModel = applicationModel;
            this._claimsService = claimService;
            this._metadataService = metadataService;
            this._claimContainer = container;
            this._eventAggregator = eventAggregator;
            this._searchManager = new SearchManager();
            // Close the search tab.
            this.ClaimSearchModel.OnCloseClick += new EventHandler(this._claimSearchModel_OnCloseClick);
            // Display the preview of the selected claim.
            this.ClaimSearchModel.SelectedRowChanged += new EventHandler<SelectedRowChangedEventArgs>(this._claimSearchModel_SelectedRowChanged);
            // Grid paging.
            this.ClaimSearchModel.NeedData += new EventHandler<SearchDataRequestEventArgs>(this._claimSearchModel_NeedData);
            this.ClaimSearchModel.RefreshView += new EventHandler(this._claimSearchModel_RefreshView);
        }

        private void _claimSearchModel_RefreshView(object sender, EventArgs e)
        {
            SearchResultModel searchResultModel = this.ClaimSearchModel;
            searchResultModel.IsBusy = true;
            searchResultModel.SelectedRow = null;
            searchResultModel.GlobalSearchRows = null;
            this.RefreshClaimsSearchData(searchResultModel.ProviderName, () =>
            {
                searchResultModel.IsBusy = false;
            });
        }

        private void _claimSearchModel_NeedData(object sender, SearchDataRequestEventArgs e)
        {
            SearchResultModel searchResultModel = this.ClaimSearchModel;
            SearchRequest currentRequest = searchResultModel.CurrentRequest;
            if (currentRequest != null)
            {
                currentRequest.PageSize = e.PageSize;
                currentRequest.RecordIndex = e.RowIndex;
                currentRequest.SortColumn = e.SortColumn;
                currentRequest.SortOrder = e.SortOrder;

                this.PerformSearch(currentRequest, 
                    searchResultModel, 
                    false, 
                    searchModel =>
                {
                    this.ClaimSearchModel.TimeOfSearch = searchModel.TimeOfSearch;
                    this.ClaimSearchModel.TotalRows = searchModel.TotalRows;
                    if (searchModel.SelectedRow != null)
                    {
                        this._claimSearchModel_SelectedRowChanged(this.ClaimSearchModel, new SelectedRowChangedEventArgs() { SelectedRows = new ObservableCollection<GlobalSearchGridRow>() { searchModel.SelectedRow } });
                    }
                });
            }
        }

        private void _claimSearchModel_SelectedRowChanged(object sender, SelectedRowChangedEventArgs e)
        {
            if (e.SelectedRows == null || e.SelectedRows.Count() == 0)
            {
                return;
            }

            GlobalSearchGridRow row = e.SelectedRows.First();
            SearchResultModel searchResultModel = this.ClaimSearchModel;
            if (searchResultModel.IsPreviewCollapsed)
            {
                searchResultModel.IsPreviewCollapsed = false;
            }

            this.LoadPreview(e.SelectedRows, null);
        }

        private void _claimSearchModel_OnCloseClick(object sender, EventArgs e)
        {
            this.Navigator.Finish();
        }

        protected void RefreshClaimsSearchData(string providerName)
        {
            this.RefreshClaimsSearchData(providerName, null);
        }

        protected void RefreshClaimsSearchData(string providerName, Action onComplete)
        {
            this.ClaimSearchModel.IsLoadingData = true;
            this.ClaimSearchModel.IsBusy = true;
            SearchResultModel searchResultModel = this.ClaimSearchModel;

            searchResultModel.GlobalSearchRows = null;

            this.UnSelectRow(searchResultModel);
            if (searchResultModel.CurrentRequest != null)
            {
                searchResultModel.CurrentRequest.PageSize = searchResultModel.PageSize;
            }
            // Initiate the duplicate search.
            this.PerformInitialSearch("ClaimSummaryAmountsSearchProvider", searchModel =>
            {
                this.ClaimSearchModel.TimeOfSearch = searchModel.TimeOfSearch;
                // this._claimSearchModel.TotalResults = searchModel.TotalRows;
                if (onComplete != null)
                {
                    onComplete();
                }

                this.ClaimSearchModel.IsBusy = false;
                this.ClaimSearchModel.IsLoadingData = false;
                searchModel.SelectedRow = searchModel.GlobalSearchRows.FirstOrDefault();
                this.LoadPreview(new[] { searchModel.SelectedRow }, null);
            });
        }

        // Set the tab title as ClaimReference - Claim title.
        private void SetHeader()
        {
            if (string.IsNullOrWhiteSpace(this.ClaimSearchModel.TabHeaderTitle))
            {
                if (string.IsNullOrWhiteSpace(this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimReference")) == false &&
                    string.IsNullOrWhiteSpace(this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimTitle")) == false)
                {
                    this.ClaimSearchModel.TabHeaderTitle = Resources.StringResources.ClaimSearchTitle + ":" + " " +
                        this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimReference") + "-" +
                        this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimTitle");
                }
                else if (string.IsNullOrWhiteSpace(this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimTitle")))
                {
                    this.ClaimSearchModel.TabHeaderTitle = Resources.StringResources.ClaimSearchTitle + ":"+ " " +
                        this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimReference");
                }
                else if (string.IsNullOrWhiteSpace(this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimReference")))
                {
                    this.ClaimSearchModel.TabHeaderTitle = Resources.StringResources.ClaimSearchTitle + ":" + " " +
                        this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimTitle");
                }
                else if (string.IsNullOrWhiteSpace(this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimReference")) &&
                    string.IsNullOrWhiteSpace(this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ClaimTitle")))
                {
                    this.ClaimSearchModel.TabHeaderTitle = Resources.StringResources.ClaimSearchTitle;
                }

                this.ClaimSearchModel.PageTitle = this.ClaimSearchModel.TabHeaderTitle;
            }
        }

        // Check the column visibilty based on the product codes set in shell configuration.
        private void DisplayColumn()
        {
            string productCode = this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ProductCode");
            if (string.IsNullOrWhiteSpace(productCode))
            {
                return;
            }

            AppModel appModel = this._claimContainer.Resolve<AppModel>();
            bool displayColumn = true;
            var confSetting = appModel.ShellConfiguration.ConfigurationSettings["ProductCodeColumnVisibility"];

            if (confSetting == null)
            {
                return;
            }

            foreach (SettingParameter parameter in confSetting.SettingParmeters)
            {
                if (parameter.QualifierName == "ProductCodes" && parameter.QualifierValue.Contains(productCode))
                {
                    displayColumn = false;
                    break;
                }
            }

            // set the property in model.
            this.ClaimSearchModel.ShowColumn = displayColumn;
        }

        private void RegisterChannel(ISearchChannel searchChannel)
        {
            this._searchManager.RegisterSearchChannel(searchChannel);
            // Initiate the duplicate search.
            this.PerformInitialSearch(searchChannel.ProviderName, searchResultModel =>
            {
            });
        }

        private SearchContext LoadSearchContext(GlobalSearchControllerArgs args)
        {
            SearchContext context = new SearchContext();
            context.ChannelCriteria = args.SearchCriteria.GetChannelCriteria();
            context.RegisterSearchChannel = this.RegisterChannel;
            // Set the task arguments in the serach request.
            this.ClaimSearchModel.SearchCriteria = args;
            return context;
        }

        protected override void LoadPreview(IEnumerable<GlobalSearchGridRow> searchGridRows, Action<PreviewData> onComplete)
        {
            if (searchGridRows.Count() > 1)
            {
                throw new NotSupportedException(XIAP.FrontendModules.Common.Resources.StringResources.MultipleSelectionNotSupported);
            }

            var row = searchGridRows.Single();

            SearchResultModel searchResultModel = this.ClaimSearchModel;
            searchResultModel.IsLoadingData = true;
            if (row != null)
            {
                this._searchManager.LoadView(row.ProviderName,
                    searchGridRows.Select(a => a.SearchRow),
                    view =>
                    {
                        this.DisplayClaimsPreview("ClaimsPreview", view, row);

                        searchResultModel.MenuItems = view.MenuItems;
                        searchResultModel.IsLoadingData = false;
                        searchResultModel.IsInitialSearch = false;

                        if (onComplete != null)
                        {
                            onComplete(view);
                        }
                    },
                    args =>
                    {
                        searchResultModel.IsLoadingData = false;
                        searchResultModel.IsPreviewCollapsed = true;
                        return false;
                    });
            }
        }

        public override void OnStart(TaskArgumentsHolder holder)
        {
            this._previewRegionHelper = new SearchPreviewRegionHelper(this.Navigator.ScopedRegionManager, "PreviewRegion");
            SearchContext context = this.LoadSearchContext(holder.ControllerArguments as GlobalSearchControllerArgs);
            this.ClaimSearchModel.BuildSearchItems(this.Navigator.ScopedRegionManager);
            this._eventAggregator.GetEvent<DuplicateSearchEvent>().Publish(context);
            
            this.SetHeader();
        }

        protected override void PerformInitialSearch(string providerName, Action<SearchResultModel> onComplete)
        {
            this.PerformInitialSearch(providerName, new List<KeyValuePair<string, object>>(), onComplete);
        }

        private void SearchAtLeastOneCritria(SearchRequest searchRequest)
        {
            searchRequest.Filters.Add(new KeyValuePair<string, object>("DclDateOfLoss", this.ClaimSearchModel.SearchCriteria.GetProperty<object>("DateOfLoss")));
            searchRequest.Filters.Add(new KeyValuePair<string, object>("ClientID", this.ClaimSearchModel.SearchCriteria.GetProperty<object>("ClientID")));
            searchRequest.Filters.Add(new KeyValuePair<string, object>("ClaimantSurname", this.ClaimSearchModel.SearchCriteria.GetProperty<object>("ClaimantSurname")));
            searchRequest.Filters.Add(new KeyValuePair<string, object>("ClientReference", this.ClaimSearchModel.SearchCriteria.GetProperty<object>("ClientReference")));
            searchRequest.Filters.Add(new KeyValuePair<string, object>("OutsourceReference", this.ClaimSearchModel.SearchCriteria.GetProperty<object>("OutsourceReference")));
            searchRequest.Filters.Add(new KeyValuePair<string, object>("DclProductCode", this.ClaimSearchModel.SearchCriteria.GetProperty<object>("ProductCode")));
            
            string productCode = this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ProductCode");

            if (productCode != AXAClaimConstants.LIABILITY_CLAIM_PRODUCT)
            {
                searchRequest.Filters.Add(new KeyValuePair<string, object>("DriverSurname", this.ClaimSearchModel.SearchCriteria.GetProperty<object>("DriverSurname")));
                searchRequest.Filters.Add(new KeyValuePair<string, object>("DclRegistrationNbr", this.ClaimSearchModel.SearchCriteria.GetProperty<object>("DclRegistrationNbr")));
            }
        }

        protected override void PerformInitialSearch(string providerName, List<KeyValuePair<string, object>> filters, Action<SearchResultModel> onComplete)
        {
            SearchResultModel resultModel = this.ClaimSearchModel;
            SearchRequest searchRequest = new SearchRequest();
            searchRequest.Filters = new List<KeyValuePair<string, object>>();

            // Set the serach filter criteria
            this.SearchAtLeastOneCritria(searchRequest);
            searchRequest.PageSize = resultModel == null || resultModel.PageSize == 0 ? 50 : resultModel.PageSize;
            searchRequest.SortColumn = this.GetSortOrder(searchRequest);
            searchRequest.SortOrder = SortOrder.Descending;
            searchRequest.RecordIndex = 0;
            searchRequest.ProviderName = providerName;
            int orgPageSize = Convert.ToInt32(searchRequest.PageSize);

            this._searchManager.PerformSearch(searchRequest,
                false,
                searchResult =>
                {
                    SearchResultModel searchResultModel = this.ClaimSearchModel;
                    if (searchResultModel != null)
                    {
                        searchResultModel.IsLoadingData = true;
                        

                        ObservableCollection<GlobalSearchGridRow> globalRows = searchResult.Results.Transform<ISearchRow, GlobalSearchGridRow>(row =>
                        {
                            return new GlobalSearchGridRow { ProviderName = providerName, SearchRow = row };
                        });

                        searchResultModel.CurrentRequest = searchRequest;
                        searchResultModel.GlobalSearchRows = globalRows;
                        if (globalRows.Count > searchResultModel.PageSize)
                        {
                            searchResultModel.GlobalSearchRows = globalRows.Take(orgPageSize).ToObservableCollection();
                        }

                        searchResultModel.TotalRows = (int)searchResult.TotalResults;
                        searchResultModel.TimeOfSearch = searchResult.TimeOfSearch;

                        if (searchResultModel.PageSize != orgPageSize)
                        {
                            // searchResultModel.PageSize = orgPageSize;
                            this.ClaimSearchModel.ClaimsSearchResultsGrid.PageSize = orgPageSize + 1;
                            searchResultModel.PageSize = orgPageSize + 1;
                            this.ClaimSearchModel.ClaimsSearchResultsGrid.Refresh();
                        }

                        searchResultModel.IsLoadingData = false;
                        this.DisplayColumn();
                        if (onComplete != null)
                        {
                            onComplete(searchResultModel);
                        }
                    }
                });
        }

        private void PerformSearch(SearchRequest request, SearchResultModel searchResultModel, bool refreshCache, Action<SearchResultModel> onComplete)
        {
            this._searchManager.PerformSearch(request,
                refreshCache,
                searchResult =>
                {
                    ObservableCollection<GlobalSearchGridRow> globalRows = searchResult.Results.Transform<ISearchRow, GlobalSearchGridRow>(row =>
                    {
                        return new GlobalSearchGridRow { ProviderName = ProviderName, SearchRow = row };
                    });

                    searchResultModel.GlobalSearchRows = globalRows;
                    searchResultModel.TotalRows = (int)searchResult.TotalResults;
                    searchResultModel.TimeOfSearch = searchResult.TimeOfSearch;
                    if (onComplete != null)
                    {
                        onComplete(searchResultModel);
                    }
                });
        }

        private string GetSortOrder(SearchRequest request)
        {
            string orderBy = string.Empty;

            if (request.Filters[0].Value != null)
            {
                orderBy += "DateOfLossFrom,";
            }

            if (request.Filters[1].Value != null)
            {
                orderBy += "Insured,";
            }

            if (request.Filters[2].Value != null)
            {
                orderBy += "Claimant,";
            }

            if (request.Filters[3].Value != null)
            {
                orderBy += "ClientReference,";
            }

            if (request.Filters[4].Value != null)
            {
                orderBy += "OutsourceReference,";
            }

            string productCode = this.ClaimSearchModel.SearchCriteria.GetProperty<string>("ProductCode");
            this.ClaimSearchModel.ProductCode = productCode;
            if (productCode != AXAClaimConstants.LIABILITY_CLAIM_PRODUCT)
            {
                if (request.Filters[5].Value != null)
                {
                    orderBy += "Driver,";
                }

                if (request.Filters[6].Value != null)
                {
                    orderBy += "RegistrationNumber,";
                }

                if (request.Filters[7].Value != null)
                {
                    orderBy += "TPRegistrationNumber,";
                }
            }

            if (orderBy.Length > 0)
            {
                orderBy = orderBy.Substring(0, orderBy.Length - 1);
            }

            return orderBy;
        }

        protected void DisplayClaimsPreview(string viewId, PreviewData previewData, GlobalSearchGridRow row)
        {
            var regionHelper = this._previewRegionHelper;
            if (!regionHelper.ContainsView(viewId))
            {
                regionHelper.AddNewView(previewData.View, viewId);
            }

            regionHelper.ActivateView(viewId);
        }

        public override void Dispose()
        {
            base.Dispose();
            this.ClaimSearchModel.OnCloseClick -= new EventHandler(this._claimSearchModel_OnCloseClick);
        }
    }
}
