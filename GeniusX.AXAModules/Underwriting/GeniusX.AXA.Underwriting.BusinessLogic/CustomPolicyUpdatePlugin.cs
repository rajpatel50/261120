using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Underwriting.BusinessLogic
{
    public class CustomPolicyUpdatePlugin : AbstractComponentPlugin
    {
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            // Update header to verified when automatically creating a policy from coverage verification
            ProcessResultsCollection results = new ProcessResultsCollection();
            if (point == ProcessInvocationPoint.Virtual)
            {
                Header header = (Header)component;
                if (header.GetProduct().Product.ExternalDataSource != null)
                {
                    var verifiedHeaderStatus = UWBusinessLogicHelper.ResolveMandatoryConfig<string>("PolicyVerifiedHeaderStatus");
                    header.HeaderStatusCode = verifiedHeaderStatus;
                }
            }

            return results;
        }
    }
}
