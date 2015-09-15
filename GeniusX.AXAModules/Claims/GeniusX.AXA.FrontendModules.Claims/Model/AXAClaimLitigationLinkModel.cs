using XIAP.Frontend.Infrastructure.QuickMenu;
using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimLitigationLinkModel : ClaimLitigationLinkModel
    {
        private bool allowAdd;
        public bool AllowAdd
        {
            get
            {
                return this.allowAdd;
            }

            set
            {
                this.allowAdd = value;
            }
        }

        public override bool CanAddLinkedLitigations(NavigationMenuItemEventArgs arg)
        {
            return this.allowAdd;
        }
    }
}
