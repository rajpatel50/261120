using System.Linq;
using System.Security;
using Xiap.Framework;
using Xiap.Framework.Extensions;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.InsuranceDirectory.BusinessLogic;
using Xiap.Metadata.BusinessComponent;

namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    /// <summary>
    /// Validates that we don't add an externally controlled name usage, i.e. maintained by Genius not GeniusX, 
    /// on to a name that already has GeniusX controlled name usages.
    /// </summary>
    public class NameUsagePlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Process on PreCreate Validation only.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="point">The point.</param>
        /// <param name="pluginId">The plugin identifier.</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<NameUsage> pluginHelper = new PluginHelper<NameUsage>(point, (NameUsage)component, new ProcessResultsCollection());
            NameUsage nameUsage = (NameUsage)component;
            switch (point)
            {
                case ProcessInvocationPoint.PreCreateValidation:
                    {                        
                        Name name = component.Parent as Name;

                        // Check this name usage isn't controlled in GeniusX
                        if (nameUsage.GetDefinitionComponent().CustomCode01 != IDConstants.NAME_CONTROLLED_IN_GENIUSX)
                        {
                            // Check the user has the rights to edit names that aren't controlled by GeniusX (will throw exception if they can't)
                            InsuranceDirectoryBusinessLogicHelper.VerifyPermissionForCurrentUser(IDConstants.GENIUS_SOURCED_NAME_MAINTENANCE_PERMISSION_TOKEN);
                            
                            if (!name.NameUsages.IsNullOrEmpty())
                            {
                                // Find the other NameUsages on the Name that are current.
                                var nameUsages = name.NameUsages.Where(nu => nu.NameUsageID != 0);

                                if (nameUsages != null && nameUsages.Count() > 0 && nameUsages.OrderBy(nu => nu.NameUsageID).First().GetDefinitionComponent().CustomCode01 == IDConstants.NAME_CONTROLLED_IN_GENIUSX)
                                {
                                    // Throw an error if we find any Name Usages on this name that ARE maintained in GeniusX
                                    string usageTypeDescription = SystemValueSetCache.GetCodeDescription(nameUsage.NameUsageTypeCode, SystemValueSetCodeEnum.NameUsageType, false);
                                    InsuranceDirectoryHelper.SetProcessResult(pluginHelper.ProcessResults, component, point, "INVALID_NAMEUSAGE",usageTypeDescription);
                                }
                            }
                        }
                       
                        break;
                    }
            }

            return pluginHelper.ProcessResults;
        }
    }
}
