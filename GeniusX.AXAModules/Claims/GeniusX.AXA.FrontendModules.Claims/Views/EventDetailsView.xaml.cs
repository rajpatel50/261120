using System;
using XIAP.Frontend.Infrastructure;

namespace GeniusX.AXA.FrontendModules.Claims.Views
{
    public partial class EventDetailsView : UserControlBase
    {
        public EventDetailsView()
        {
            this.InitializeComponent();
        }

        private void Navigator_OnNavigateCompleted(object sender, EventArgs e)
        {
            this.UpdateLayout();
        }

        public override void BindData()
        {
            this.Controller.Navigator.OnNavigateCompleted -= new EventHandler(this.Navigator_OnNavigateCompleted);
            this.Controller.Navigator.OnNavigateCompleted += new EventHandler(this.Navigator_OnNavigateCompleted);
        }

        public override void Dispose()
        {
            this.Controller.Navigator.OnNavigateCompleted -= new EventHandler(this.Navigator_OnNavigateCompleted);
        }
    }
}
