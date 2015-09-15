using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure.QuickMenu;
using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimDetailLinkModel : ClaimDetailLinkModel
    {
        private AXAClaimModel model;
        public AXAClaimDetailLinkModel(AXAClaimModel claimModel)
        {
            this.model = claimModel;
        }

        protected override bool CanAddClaimDetail(NavigationMenuItemEventArgs arg)
        {
            bool canAdd = base.CanAddClaimDetail(arg);
            if (canAdd)
            {
                if (this.model.ParentClaimInvolvementData != null && this.model.ParentClaimInvolvementData.ClaimInvolvementType != (short)StaticValues.LinkableComponentType.NameInvolvement)
                {
                    canAdd = false;
                }
            }

            return canAdd;
        }
    }
}
