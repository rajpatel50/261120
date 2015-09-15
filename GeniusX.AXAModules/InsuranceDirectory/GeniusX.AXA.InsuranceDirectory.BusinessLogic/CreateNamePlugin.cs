using System.Linq;
using Xiap.Framework;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;

namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    /// <summary>
    /// If the Name is maintained outside of Xuber, this sets the Name Reference on the Name which is used to hold the ID
    /// of the Name in the external system—to be editable so a user can enter a value.
    /// </summary>
    public class CreateNamePlugin : AbstractComponentPlugin
    {
        public override ProcessResultsCollection FieldRetrieval(IBusinessComponent component, ProcessInvocationPoint point, ref Field field, int pluginId)
        {
            Name name = component as Name;

            if (field.PropertyName == IDConstants.NAME_REFERENCE && name.NameUsages.Count > 0)
            {
                if (name.NameUsages.OrderBy(n=>n.NameUsageID).First().GetDefinitionComponent().CustomCode01 == IDConstants.NAME_CONTROLLED_OUTSIDE_GENIUSX
                    && component.IsEditable)
                {
                    field.Readonly = false;
                }
                else
                {
                    field.Readonly = true;
                }
            }

            return null;
        }
    }
}
