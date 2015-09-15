using System.Windows;
using GeniusX.AXA.FrontendModules.Claims.Controller;
using GeniusX.AXA.FrontendModules.Claims.Model;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.CoreControls;

namespace GeniusX.AXA.FrontendModules.Claims.Views
{
    public partial class ConfirmInactiveRecoveryClaimDetailView : XIAPPanelBase
    {
        public ConfirmInactiveRecoveryClaimDetailView()
        {
            this.InitializeComponent();
        }

        public override void BindData()
        {
            AXAClaimController ctrl = this.Controller as AXAClaimController;
            AXAClaimModel model = ctrl.Model as AXAClaimModel;
            this.DataContext = model;
        }
    }
}

