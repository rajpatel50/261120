using XIAP.FrontendModules.Underwriting.Model.Search;


namespace GeniusX.AXA.FrontendModules.Underwriting.Model.Search
{
    public class AXAPolicySearchModel : UWSearchModel
    {
        public AXAPolicySearchModel()
        {
        }

        // GeniusPolicySearch rename by IsExternalPolicySearch
        public override bool IsExternalPolicySearch
        {
            get
            {
                return true;
            }
        }
    }
}
