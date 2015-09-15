using System.Windows;
using GeniusX.AXA.FrontendModules.Claims.Controller;
using GeniusX.AXA.FrontendModules.Claims.Model;
using XIAP.Frontend.Infrastructure;

namespace GeniusX.AXA.FrontendModules.Claims.Views
{
    public partial class DeductibleReasonCodesView : UserControlPopupBase
    {
        public DeductibleReasonCodesView()
        {
            this.InitializeComponent();
        }

        public override void BindData()
        {
            this.DataContext = ((AXADeductibleReasonCodeController)this.Controller).Model;
        }

        private void Dialog_Header_CloseClicked(object sender, RoutedEventArgs e)
        {
            DeductibleReasonCodeModel model = this.Controller.Model as DeductibleReasonCodeModel;
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
