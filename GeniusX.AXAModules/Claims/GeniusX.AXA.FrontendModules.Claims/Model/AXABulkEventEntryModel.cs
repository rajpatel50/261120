using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXABulkEventEntryModel : BulkEventEntryModel
    {
        public override string SearchControllerName
        {
            get
            {
                return "AXAClaimPopupSearchController";
            }
        }
    }
}
