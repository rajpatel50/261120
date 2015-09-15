using GeniusX.AXA.FrontendModules.Claims.Controller;
using GeniusX.AXA.FrontendModules.Claims.Model;
using XIAP.Frontend.CoreControls;

namespace GeniusX.AXA.Claims.FrontendModules.Views
{
    public partial class ClaimTotalsView : XIAPPanelBase
    {
        public ClaimTotalsView()
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
