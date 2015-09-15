using System.Windows;
using GeniusX.AXA.FrontendModules.Claims.Controller;
using GeniusX.AXA.FrontendModules.Claims.Model;
using XIAP.Frontend.Infrastructure;

namespace GeniusX.AXA.FrontendModules.Claims.Views
{
    public partial class ClaimPaymentRejectReasonView : UserControlBase
    {
        public ClaimPaymentRejectReasonView()
        {
            this.InitializeComponent();
        }

		public override void BindData()
		{
            this.DataContext = ((AXAManualAuthorisationController)this.Controller).AuthorisationModel;
		}

		private void Dialog_Header_CloseClicked(object sender, RoutedEventArgs e)
		{
            ClaimPaymentRejectReasonModel model = (this.Controller as AXAManualAuthorisationController).AuthorisationModel;
			if (model.CanCancel(null))
			{
				model.Cancel(null);
			}
		}

		public override void Dispose()
		{
			base.Dispose();
		}
    }
}
