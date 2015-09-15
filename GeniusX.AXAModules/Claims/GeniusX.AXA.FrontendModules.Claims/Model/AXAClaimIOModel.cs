using XIAP.FrontendModules.Claims.Model.ClaimIO;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimIOModel : ClaimIOModel
    {
        protected override bool CanAddInsuredObject(object args)
        {
            return false;
        }
    }
}
