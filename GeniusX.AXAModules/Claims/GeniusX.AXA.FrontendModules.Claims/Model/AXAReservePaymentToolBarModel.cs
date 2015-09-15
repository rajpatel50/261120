using XIAP.Frontend.Infrastructure.QuickMenu;
using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAReservePaymentToolBarModel : ReservePaymentToolBarModel
    {
        private AXAClaimModel _claimModel;
        public AXAReservePaymentToolBarModel(AXAClaimModel claimModel) : base()
        {
            this._claimModel = claimModel;
        }

        public override bool CanMakePaymentClick(NavigationMenuItemEventArgs args)
        {
            if (this._claimModel.HeaderData.ClaimHeaderStatusCode == AXAClaimConstants.REPORT_ONLY_NO_ESTIMATE_STATUS || this._claimModel.HeaderData.ClaimHeaderStatusCode == AXAClaimConstants.REPORT_ONLY_ESTIMATE_MADE_STATUS)
            {
                return false;
            }

            return base.CanMakePaymentClick(args);
        }

        public override bool CanMakeRecoveryReceiptClick(NavigationMenuItemEventArgs args)
        {
            if (this._claimModel.HeaderData.ClaimHeaderStatusCode == AXAClaimConstants.REPORT_ONLY_NO_ESTIMATE_STATUS || this._claimModel.HeaderData.ClaimHeaderStatusCode == AXAClaimConstants.REPORT_ONLY_ESTIMATE_MADE_STATUS)
            {
                return false;
            }

            return base.CanMakeRecoveryReceiptClick(args);
        }
    }
}
