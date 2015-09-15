using XIAP.Frontend.CoreControls;
using XIAP.FrontendModules.Underwriting.Controller;

namespace GeniusX.AXA.FrontendModules.Underwriting.Views.Header
{
    public partial class MainDetailsPanel : XIAPPanelBase
    {
        public MainDetailsPanel()
        {
            this.InitializeComponent();
        }

        protected override void XIAPPanelBaseBindData()
        {
            (this.Controller as RiskController).LoadHeader();
        }

		public override void Dispose()
		{
            this.LayoutRoot.Children.Clear();
            this.LayoutRoot = null; 

			this._validatorManager.Clear();
			this._validatorManager = null;
			base.Dispose();
		}
    }
}
