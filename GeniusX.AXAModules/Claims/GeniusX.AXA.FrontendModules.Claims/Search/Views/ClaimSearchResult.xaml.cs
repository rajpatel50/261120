using System.Collections.Generic;
using System.Linq;
using ComponentArt.Silverlight.UI.Data;
using GeniusX.AXA.FrontendModules.Claims.Search.Controller;
using GeniusX.AXA.FrontendModules.Claims.Search.Model;
using XIAP.Frontend.CoreControls;

namespace GeniusX.AXA.FrontendModules.Claims.Search.Views
{
    public partial class ClaimSearchResult : XIAPPanelBase
    {
        public ClaimSearchResult()
        {
            this.InitializeComponent();
        }

        public override void BindData()
        {
            base.BindData();
            // Model have changed from ClaimSearchModel to SearchResultsModel 
            this.DataContext = (this.Controller as ClaimSearchController).ClaimSearchModel;
            (this.Controller as ClaimSearchController).ClaimSearchModel.ClaimsSearchResultsGrid = this.ClaimSearchResultGrid;
        }

        public override void Dispose()
        {
            this.LayoutRoot.Children.Clear();
            this.LayoutRoot = null;
            base.Dispose();
            this.ClaimSearchResultGrid.DataContext = null;
            this.ClaimSearchResultGrid.ItemsSource = null;
            this.ClaimSearchResultGrid.Dispose();
        }

        protected void SearchResultsGrid_RowLoaded(object sender, ComponentArt.Silverlight.UI.Data.DataGridRowEventArgs e)
        {
            if (this.ClaimSearchResultGrid.SelectedRow == null || (this.ClaimSearchResultGrid.LoadedRows != null && this.ClaimSearchResultGrid.LoadedRows.Count == 1))
            {
                this.ClaimSearchResultGrid.ScrollTo(0);
                this.ClaimSearchResultGrid.SelectRow(0);
                this.ClaimSearchResultGrid.Focus();
            }
        }

        private void SearchResultsGrid_SortingChanged(object sender, DataGridSortingChangedEventArgs e)
        {
            if (e.Sortings.Count > 0)
            {
                List<AXAClaimSearchRow> orderedList = null;
                string sortingColumnName = e.Sortings[0].Column.ColumnName;

                if (e.Sortings[0].Direction == SortDirection.Ascending)
                {
                    switch (e.Sortings[0].Column.ColumnName)
                    {
                        case "ClaimReference":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.ClaimReference).ToList();
                            break;
                        case "ClaimTitle":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.ClaimTitle).ToList();
                            break;
                        case "Claimant":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.Claimant).ToList();
                            break;
                        case "DateOfLossFrom":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.DateOfLossFrom).ToList();
                            break;
                        case "Insured":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.Insured).ToList();
                            break;
                        case "TPRegistrationNumber":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.TPRegistrationNumber).ToList();
                            break;
                        case "Driver":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.Driver).ToList();
                            break;
                        case "RegistrationNumber":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.RegistrationNumber).ToList();
                            break;
                        case "ClientReference":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.ClientReference).ToList();
                            break;
                        case "OutsourceReference":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderBy(x => x.OutsourceReference).ToList();
                            break;
                    }
                }
                else
                {
                    switch (e.Sortings[0].Column.ColumnName)
                    {
                        case "ClaimReference":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.ClaimReference).ToList();
                            break;
                        case "ClaimTitle":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.ClaimTitle).ToList();
                            break;
                        case "Claimant":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.Claimant).ToList();
                            break;
                        case "DateOfLossFrom":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.DateOfLossFrom).ToList();
                            break;
                        case "Insured":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.Insured).ToList();
                            break;
                        case "TPRegistrationNumber":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.TPRegistrationNumber).ToList();
                            break;
                        case "Driver":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.Driver).ToList();
                            break;
                        case "RegistrationNumber":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.RegistrationNumber).ToList();
                            break;
                        case "ClientReference":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.ClientReference).ToList();
                            break;
                        case "OutsourceReference":
                            orderedList = this.ClaimSearchResultGrid.ItemsSource.Cast<AXAClaimSearchRow>().OrderByDescending(x => x.OutsourceReference).ToList();
                            break;
                    }
                }

                if (orderedList != null)
                {
                    this.ClaimSearchResultGrid.ItemsSource = orderedList;
                }
            }
        }
    }
}
