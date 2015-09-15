using XIAP.Frontend.CoreControls;
using XIAP.FrontendModules.Claims.Controller;

namespace GeniusX.AXA.FrontendModules.Claims.Views
{
    public partial class ClaimAmountHistorySearch : XIAPPanelBase
    {
        public ClaimAmountHistorySearch()
        {
            this.InitializeComponent();
        }

        public override void BindData()
        {
            this.DataContext = (this.Controller as ClaimController).ClaimAmountHistoryController.Model;
        }
    }
}
