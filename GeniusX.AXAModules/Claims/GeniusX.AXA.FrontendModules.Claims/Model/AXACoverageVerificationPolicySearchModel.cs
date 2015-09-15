using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXACoverageVerificationPolicySearchModel : CoverageVerificationPolicySearchModel
    {
        public AXACoverageVerificationPolicySearchModel()
            : base()
        {
        }

        protected override bool CanSearchStart(object args)
        {
            return base.CanSearchStart(args) && this.IsPolicyExist;
        }

        public override bool CanEnabled(object parameter)
        {
            return base.CanEnabled(parameter) && this.IsPolicyExist;
        }
    }
}
