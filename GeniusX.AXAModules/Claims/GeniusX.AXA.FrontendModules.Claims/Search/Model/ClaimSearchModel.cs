using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Regions;
using XIAP.Frontend.CoreControls;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Converter;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.Frontend.Infrastructure.Utils;
using XIAP.FrontendModules.Claims.Search.Model;
using XIAP.FrontendModules.Search.Controller;
using XIAP.FrontendModules.Search.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Search.Model
{
    /// <summary>
    /// Duplicate claim serach model class.
    /// </summary>
    public class ClaimSearchModel : SearchResultModel
    {
        private GlobalSearchControllerArgs _uiSearchCriteria;
        private bool _showColumn;
        private string _productCode;
        private XIAPSearchResultGrid _claimsSearchResultsGrid;
        private DelegateCommand<object> _refresh;
        private IDelayedEvent<SelectedRowChangedEventArgs> _timer;
        private EventHandler<SelectedRowChangedEventArgs> _completedHandler;

        public ClaimSearchModel(IDelayedEvent<SelectedRowChangedEventArgs> timer)
        {
            this._timer = timer;
            this.NeedDataCommand = new DelegateCommand<XIAPGridNeedDataEventArgs>((arg) => InvokeEvent<SearchDataRequestEventArgs>(this.NeedData, this.BuildSearchRequestEventArgs(arg)));
            this.Refresh = new DelegateCommand<object>(this.DoRefresh, (c) => { return !this.IsBusy; });
            this.SelectionChangedCommand = new DelegateCommand<IList>(rows => this.BeginDelayedEvent(rows));
            this._completedHandler = (sender, e) => InvokeEvent<SelectedRowChangedEventArgs>(this.SelectedRowChanged, e);
            this._timer.Completed += this._completedHandler;
        }

        public event EventHandler<SelectedRowChangedEventArgs> SelectedRowChanged;
        public event EventHandler RefreshView;
        public event EventHandler<SearchDataRequestEventArgs> NeedData;

        ////Search criteria.
        public GlobalSearchControllerArgs SearchCriteria
        {
            get
            {
                return this._uiSearchCriteria;
            }

            set
            {
                this._uiSearchCriteria = value;
            }
        }

        public DelegateCommand<XIAPGridNeedDataEventArgs> NeedDataCommand { get; private set; }
        public DelegateCommand<IList> SelectionChangedCommand { get; private set; }

        ////Column visibility (configurable through shell.config)
        public bool ShowColumn
        {
            get
            {
                return this._showColumn;
            }

            set
            {
                this._showColumn = value;
            }
        }

        ////Grid object 
        public XIAPSearchResultGrid ClaimsSearchResultsGrid
        {
            get
            {
                return this._claimsSearchResultsGrid;
            }

            set
            {
                this._claimsSearchResultsGrid = value;
            }
        }

        public DelegateCommand<object> Refresh
        {
            get
            {
                return this._refresh;
            }

            set
            {
                this._refresh = value;
                OnPropertyChanged("Refresh");
            }
        }

        public override string TabHeaderTitle
        {
            get
            {
                int rowCount = 0;
                if (!this.GlobalSearchRows.IsNullOrEmpty() && this.GlobalSearchRows.FirstOrDefault().SearchRow != null)
                {
                    rowCount = (this.GlobalSearchRows.FirstOrDefault().SearchRow as AXAClaimSearchRow).TotalClaimsCount;
                }

                if (this.IsLoadingData == false)
                {
                    return base.TabHeaderTitle.Split('(')[0] + "(" + rowCount.ToString("#,##0") + ")";
                }
                else
                {
                    return base.TabHeaderTitle;
                }
            }

            set
            {
                base.TabHeaderTitle = value;
                this.OnPropertyChanged("TabHeaderTitle");
            }
        }

        public string ProductCode
        {
            get 
            {
                return this._productCode;
            }

            set
            {
                this._productCode = value;
                this.OnPropertyChanged("ProductCode");
                this.BindColumnVisibility(this.ClaimsSearchResultsGrid);
            }
        }

        public void DoRefresh(object args)
        {
            InvokeEvent(this.RefreshView);
        }

        ////Binds the search grid with the model.
        public void BindSearchGrid(XIAPSearchResultGrid grid, SearchResultModel searchResultModel)
        {
            XIAPSearchResultGrid searchGrid = grid;
            grid.DataContext = searchResultModel;
            searchGrid.DataContext = searchResultModel;             
            searchResultModel.CloseButtonVisibility = Visibility.Visible;
            searchResultModel.CloseCommand = this.CloseCommand;
            searchResultModel.IsLoadingData = true;

            searchGrid.NeedData += (sender, args) =>
            {
                if (this.NeedDataCommand.CanExecute(args))
                {
                    args.GridID = searchResultModel.ProviderName;
                    this.NeedDataCommand.Execute(args);
                }
            };
           
            searchGrid.SetBinding(XIAPReadOnlyGrid.SelectedItemProperty,new Binding("SelectedRow")
            {
                Mode = BindingMode.TwoWay,
                Source = searchResultModel
            });

            searchGrid.AllowPaging = true;
            searchGrid.AutoAdjustPageSize = true;
            searchGrid.FooterVisibility = Visibility.Visible;
            searchGrid.SetBinding(XIAPReadOnlyGrid.ShowFooterInfoProperty, new Binding("IsInitialSearch")
            {   
                Converter = new InvertBooleanConverter(),
                Source = this               
            });

            searchGrid.ShowRefreshInfo = true;
            searchGrid.SetBinding(XIAPReadOnlyGrid.RefreshInfoTextProperty, new Binding("RefreshInfoText")
            {
                Source = searchResultModel
            });
            searchGrid.RefreshCommand = this.Refresh;
            this.BindColumnVisibility(searchGrid);        
        }

        public void BuildSearchItems(IRegionManager regionManager)
        {
            if (regionManager != null)
            {
                XIAPSearchResultGrid grid = this.ClaimsSearchResultsGrid;
                this.BindSearchGrid(grid, this);
            }
        }

        protected SearchDataRequestEventArgs BuildSearchRequestEventArgs(XIAPGridNeedDataEventArgs arg)
        {
            return new SearchDataRequestEventArgs
            {
                ProviderName = arg.GridID,
                RowIndex = arg.RecordIndex,
                PageSize = arg.RowCount,
                SortColumn = this.ResolveSortColumn(arg.Sorting),
                SortOrder = this.ResolveSortOrder(arg.Sorting),
                Filters = arg.Filters
            };
        }

        ////Set the visibilty of the grid column based on the showcolumn model property.
        private void BindColumnVisibility(XIAPSearchResultGrid grid)
        {
            if (grid != null)
            {
                foreach (var gridColumn in grid.Columns)
                {
                    if ((gridColumn.Header.ToString() == GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Resource_Driver ||
                       gridColumn.Header.ToString() == GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Resource_ClaimClientRegistrationNumber ||
                        gridColumn.Header.ToString() == GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Resource_ClaimTPRegistrationNumber) &&
                       this.ShowColumn == false)
                    {
                        gridColumn.Visibility = Visibility.Collapsed;
                    }

                    if (((gridColumn.Header as TextBlock).Text == GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Resource_Driver ||
                        (gridColumn.Header as TextBlock).Text == GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Resource_ClaimClientRegistrationNumber ||
                        (gridColumn.Header as TextBlock).Text == GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Resource_ClaimTPRegistrationNumber) &&
                        this.ProductCode == AXAClaimConstants.LIABILITY_CLAIM_PRODUCT)
                    {
                        gridColumn.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private string ResolveSortColumn(string sortParam)
        {
            if (string.IsNullOrEmpty(sortParam))
            {
                return sortParam;
            }

            string[] sortParams = sortParam.Split(' ');
            if (sortParams.Length != 2)
            {
                throw new ArgumentException("sortParams not of correct format : " + sortParam);
            }

            if (sortParams[0].StartsWith("SearchRow."))
            {
                return sortParams[0].Substring("SearchRow.".Length);
            }

            return sortParams[0];
        }

        private SortOrder ResolveSortOrder(string sortParam)
        {
            return sortParam.EndsWith("DESC") ? SortOrder.Descending : SortOrder.Ascending;
        }

        private void BeginDelayedEvent(IList selectedRows)
        {
            var searchRows = new List<GlobalSearchGridRow>();
            foreach (var row in selectedRows)
            {
                searchRows.Add((GlobalSearchGridRow)row);
            }

            var selectedRowEventArgs = new SelectedRowChangedEventArgs
            {
                SelectedRows = searchRows
            };
            this._timer.Stop();
            this._timer.Begin(selectedRowEventArgs);
        }
    }
}
