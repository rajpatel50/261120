using XIAP.Frontend.Infrastructure.QuickMenu;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimDetailModel : ClaimDetailModel
    {
        public AXAClaimDetailModel(ClaimModel ClaimModel)
            : base(ClaimModel)
        {
            // base.AddClaimDetailCommand = new DelegateCommand<NavigationMenuItemEventArgs>(base.OnAddClaimDetail, this.CanAddClaimDetail1);
        }

        public override bool CanAddClaimDetail(AddClaimDetailEventArgs arg)
        {
            return false;
        }

        public override bool CanMakePaymentClaimDetail(NavigationMenuItemEventArgs args)
        {
            if (this.ClaimModel.HeaderData.ClaimHeaderStatusCode == AXAClaimConstants.REPORT_ONLY_NO_ESTIMATE_STATUS || this.ClaimModel.HeaderData.ClaimHeaderStatusCode == AXAClaimConstants.REPORT_ONLY_ESTIMATE_MADE_STATUS)
            {
                return false;
            }

            return base.CanMakePaymentClaimDetail(args);
        }

        public override bool CanMakeRecoveryReceiptClaimDetail(NavigationMenuItemEventArgs args)
        {
             if (this.ClaimModel.HeaderData.ClaimHeaderStatusCode == AXAClaimConstants.REPORT_ONLY_NO_ESTIMATE_STATUS || this.ClaimModel.HeaderData.ClaimHeaderStatusCode == AXAClaimConstants.REPORT_ONLY_ESTIMATE_MADE_STATUS)
            {
                return false;
            }

            return base.CanMakeRecoveryReceiptClaimDetail(args);
        }
    }
}
