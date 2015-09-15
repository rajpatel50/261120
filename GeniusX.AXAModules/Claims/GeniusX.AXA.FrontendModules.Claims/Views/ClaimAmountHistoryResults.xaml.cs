using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.CoreControls;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Resources;


namespace GeniusX.AXA.FrontendModules.Claims.Views
{
    public partial class ClaimAmountHistoryResults : XIAPPanelBase
    {
        public ClaimAmountHistoryResults()
        {
            this.InitializeComponent();
        }

        public override void BindData()
        {
            this.DataContext = (this.Controller as ClaimController).ClaimAmountHistoryController.Model;
        }

        private void SetColumnsVisibility(ClaimAmountHistoryModel model, XIAPSearchResultGrid claimAmountHistoryXIAPGrid)
        {
            Visibility visibility = Visibility.Visible;

            if (model.ClaimAmountSearchData.Payments == false)
            {
                visibility = Visibility.Collapsed;
            }

            claimAmountHistoryXIAPGrid.Columns["PayeeName"].Visibility = visibility;
            claimAmountHistoryXIAPGrid.Columns["PaymentStatusDescription"].Visibility = visibility;
            claimAmountHistoryXIAPGrid.Columns["PaymentCustomReference"].Visibility = visibility;
        }

        private void ClaimAmountHistoryXIAPGrid_RowLoaded(object sender, ComponentArt.Silverlight.UI.Data.DataGridRowEventArgs e)
        {
            string currencyCode = null;
            XIAPSearchResultGrid claimAmountHistoryXIAPGrid = this.LayoutRoot.Content as XIAPSearchResultGrid;
            claimAmountHistoryXIAPGrid.CurrentPageIndex = 0;
            ClaimAmountHistoryModel model = this.DataContext as ClaimAmountHistoryModel;
            if (model != null)
            {
                if (model.ClaimAmountSearchRows != null && model.ClaimAmountSearchRows.Count() > 0)
                {
                    currencyCode = model.ClaimAmountSearchRows.FirstOrDefault().CurrencyCode;
                }

                if (model.SelectedDisplayLevel == StaticValues.ClaimAmountHistoryDisplayLevel.Transactions.ToString())
                {
                    this.SetColumnsVisibility(model, claimAmountHistoryXIAPGrid);
                    (claimAmountHistoryXIAPGrid.Columns["PaymentReceiptAmount"].Header as TextBlock).Text = String.IsNullOrEmpty(currencyCode) == true ? StringResources.AmountHistory_PaymentReceiptAmount :
                    string.Format("{0} ({1})", StringResources.AmountHistory_PaymentReceiptAmount, currencyCode);
                    (claimAmountHistoryXIAPGrid.Columns["ReserveAmount"].Header as TextBlock).Text = String.IsNullOrEmpty(currencyCode) == true ? StringResources.AmountHistory_ReserveMovement :
                    string.Format("{0} ({1})", StringResources.AmountHistory_ReserveMovement, currencyCode);
                }

                if (model.SelectedDisplayLevel == StaticValues.ClaimAmountHistoryDisplayLevel.ClaimDetails.ToString())
                {
                    this.SetColumnsVisibility(model, claimAmountHistoryXIAPGrid);
                    if ((model.ClaimAmountSearchData.CurrencyType == (int)StaticValues.ClaimCurrencyType.ClmCcy || model.ClaimAmountSearchData.CurrencyType == (int)StaticValues.ClaimCurrencyType.BaseCcy) && model.ProductClaimDefinitionItem.IncurredAmountDerivationMethod != (short)StaticValues.IncurredAmountDerivationMethod.PaymentsReservesincludingEstimated)
                    {
                        claimAmountHistoryXIAPGrid.Columns["IncurredAmount"].Visibility = Visibility.Visible;
                    }
                    else
                    {
                        claimAmountHistoryXIAPGrid.Columns["IncurredAmount"].Visibility = Visibility.Collapsed;
                    }

                    (claimAmountHistoryXIAPGrid.Columns["PaymentReceiptAmount"].Header as TextBlock).Text = String.IsNullOrEmpty(currencyCode) == true ? StringResources.AmountHistory_PaymentReceiptAmount :
                    string.Format("{0} ({1})", StringResources.AmountHistory_PaymentReceiptAmount, currencyCode);
                    (claimAmountHistoryXIAPGrid.Columns["ReserveAmount"].Header as TextBlock).Text = String.IsNullOrEmpty(currencyCode) == true ? StringResources.AmountHistory_ReserveMovement :
                    string.Format("{0} ({1})", StringResources.AmountHistory_ReserveMovement, currencyCode);
                    (claimAmountHistoryXIAPGrid.Columns["IncurredAmount"].Header as TextBlock).Text = String.IsNullOrEmpty(currencyCode) == true ? StringResources.AmountHistory_ClaimDetailIncurredPosition :
                    string.Format("{0} ({1})", StringResources.AmountHistory_ClaimDetailIncurredPosition, currencyCode);
                }

                if (model.SelectedDisplayLevel == StaticValues.ClaimAmountHistoryDisplayLevel.Amounts.ToString())
                {
                    (claimAmountHistoryXIAPGrid.Columns["TransactionAmount"].Header as TextBlock).Text = String.IsNullOrEmpty(currencyCode) == true ? StringResources.AmountHistory_Amount :
                    string.Format("{0} ({1})", StringResources.AmountHistory_Amount, currencyCode);
                    (claimAmountHistoryXIAPGrid.Columns["MovementAmount"].Header as TextBlock).Text = String.IsNullOrEmpty(currencyCode) == true ? StringResources.AmountHistory_Movement :
                    string.Format("{0} ({1})", StringResources.AmountHistory_Movement, currencyCode);
                }
            }

            this.LayoutRoot.UpdateLayout();
        }
    }
}
