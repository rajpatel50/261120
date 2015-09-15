using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimRecoveryModel : ClaimRecoveryModel
    {
        protected override bool CanAddRecovery(object item)
        {
            return false;
        }
    }
}
