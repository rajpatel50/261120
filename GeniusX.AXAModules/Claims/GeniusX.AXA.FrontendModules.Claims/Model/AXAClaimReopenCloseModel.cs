using System.Linq;
using Xiap.Framework.Metadata;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common.ClaimService;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimReopenCloseModel : ClaimReopenCloseModel
    {
        public AXAClaimReopenCloseModel() : base()
        {
        }

        public override void SetDefaultStatus()
        {
            if (this.WizardMode == XIAP.FrontendModules.Common.WizardModeEnum.ReOpen)
            {
                if (this.ClaimResponse != null && this.ClaimResponse.ClaimHeaderDto != null)
                {
                    string selectedCode = this.GetReopenClaimHeaderStatus();
                    CodeRow code = this.AvailableStatusCodes.Where(a => a.Code == selectedCode).FirstOrDefault();
                    if (code != null && code.Code != null)
                    {
                        this.SelectedStatusCode = code.Code;
                        this.ClaimsFilterFields.SetStatusFieldReadOnly();
                    }
                }
            }
            else
            {
                base.SetDefaultStatus();
            }
        }

        private string GetReopenClaimHeaderStatus()
        {
            const string ClosedStatus = "CCL";
            const string Reopen_PaymentsMade = "CPY";
            const string Reopen_EstimateMade = "CES";
            const string Reopen_NoEstimate = "COU";
            const string ReportOnly_Closed = "CRL";
            const string Reopen_ReportOnly_EstimateMade = "CRE";
            const string Reopen_ReportOnly_NoEstimate = "CRO";

            string selectedCode = null;
            ClaimHeaderData headerData = this.ClaimResponse.ClaimHeaderDto.Data as ClaimHeaderData;

            if (headerData != null)
            {
                if (headerData.ClaimHeaderStatusCode == ClosedStatus)
                {
                    if (headerData.HasPayment)
                    {
                        selectedCode = Reopen_PaymentsMade;
                    }
                    else if (headerData.HasReserve)
                    {
                        selectedCode = Reopen_EstimateMade;
                    }
                    else
                    {
                        selectedCode = Reopen_NoEstimate;
                    }
                }
                else if (headerData.ClaimHeaderStatusCode == ReportOnly_Closed)
                {
                    if (headerData.HasReserve)
                    {
                        selectedCode = Reopen_ReportOnly_EstimateMade;
                    }
                    else
                    {
                        selectedCode = Reopen_ReportOnly_NoEstimate;
                    }
                }
            }

            return selectedCode;
        }
    }
}
