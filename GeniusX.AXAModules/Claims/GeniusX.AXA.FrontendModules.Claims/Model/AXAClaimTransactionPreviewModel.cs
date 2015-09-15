using XIAP.FrontendModules.Claims.Search.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimTransactionPreviewModel : ClaimTransactionPreviewModel
    {
        public AXAClaimTransactionPreviewModel() 
            : base()
        {
        }

        public override bool RecoveryReserveAmountsVisibility
        {
            get
            {
                return false;
            }
        }

        public override bool ReserveAmountsVisibility
        {
            get
            {
                return false;
            }
        }

        public override bool PaymentAmountsVisibility
        {
            get
            {
                return false;
            }
        }

        public override bool ReceiptAmountsVisibility
        {
            get
            {
                return false;
            }
        }
    }
}
