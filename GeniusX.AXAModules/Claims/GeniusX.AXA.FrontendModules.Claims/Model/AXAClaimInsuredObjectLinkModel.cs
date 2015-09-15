using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimInsuredObjectLinkModel : ClaimInsuredObjectLinkModel
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

        public override bool CanAddClaimIOLinks(ClaimIOMenuEventArgs args)
        {
            return this.allowAdd;
        }
    }
}