using System.Windows;
using System.Windows.Controls;
using GeniusX.AXA.FrontendModules.Claims.Controller;
using GeniusX.AXA.FrontendModules.Claims.Model;
using XIAP.Frontend.Infrastructure;

namespace GeniusX.AXA.FrontendModules.Claims.Views
{
    public partial class ClaimTaskInitialDueDateView : UserControlBase
    {
		public ClaimTaskInitialDueDateView()
        {
            this.InitializeComponent();
			this.Loaded += (e, a) => ((ContentControl)this.Parent).Visibility = System.Windows.Visibility.Collapsed;
        }

		public override void BindData()
		{
			this.DataContext = this.Controller.Model;
			((ClaimTaskInitialDueDateModel)this.Controller.Model).OnVisibilityChanged += this.ClaimTaskInitialDueDateView_OnVisibilityChanged;
		}


	   public void ClaimTaskInitialDueDateView_OnVisibilityChanged(object sender, VisibilityEventArgs e)
		{
			((ContentControl)this.Parent).Visibility = e.Visibility;
		}

		private void Dialog_Header_CloseClicked(object sender, RoutedEventArgs e)
		{
			ClaimTaskInitialDueDateModel model = this.Controller.Model as ClaimTaskInitialDueDateModel;
			if (model.CanCancel(null))
			{
				model.Cancel(null);
			}
		}

		public override void Dispose()
		{
			((ClaimTaskInitialDueDateModel)this.Controller.Model).OnVisibilityChanged -= this.ClaimTaskInitialDueDateView_OnVisibilityChanged;
			base.Dispose();
		}
    }
}
