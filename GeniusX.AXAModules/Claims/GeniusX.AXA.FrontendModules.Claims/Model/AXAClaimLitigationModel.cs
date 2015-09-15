using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common.ClaimService;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimLitigationModel : ClaimLitigationModel
    {
        public override bool CanDeleteClaimLitigation(ClaimLitigationDto arg)
        {
            return false;
        }
    }
}
