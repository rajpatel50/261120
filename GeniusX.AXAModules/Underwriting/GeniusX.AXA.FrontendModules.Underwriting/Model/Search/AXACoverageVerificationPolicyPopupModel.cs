using XIAP.FrontendModules.Underwriting.Model.Search;

namespace GeniusX.AXA.FrontendModules.Underwriting.Model.Search
{
    public class AXACoverageVerificationPolicyPopupModel : CoverageVerificationPolicyPopupModel
    {
        public AXACoverageVerificationPolicyPopupModel()
        { 
        }

        public override bool ClearInsured
        {
            get
            {
                return false;
            }
        }
    }
}
