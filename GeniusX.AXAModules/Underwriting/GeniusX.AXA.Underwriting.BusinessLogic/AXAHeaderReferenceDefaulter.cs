using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Underwriting.BusinessLogic
{
    public class AXAHeaderReferenceDefaulter : AbstractComponentPlugin
    {
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            // Plugin is configured to run after core HeaderReferenceDefaulter
            ProcessResultsCollection results = new ProcessResultsCollection();
            Header header = (Header)component;
            if (point == ProcessInvocationPoint.Created)
            {
                if (!string.IsNullOrEmpty(header.GetProduct().Product.ExternalDataSource))
                {
                    // Reset to blank, header ref will be entered manually
                    header.HeaderReference = string.Empty;
                }
            }

            return results;
        }
    }
}
