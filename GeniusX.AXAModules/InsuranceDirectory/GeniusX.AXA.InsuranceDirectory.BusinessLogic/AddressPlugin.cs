using System.Linq;
using Xiap.Framework;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;

/// <summary>
/// If the Name is maintained outside of Xuber, this sets CustomRefernce04 on the Address which is used to hold
/// the ID of the Address in the external system. 
/// Allows the user to enter a value.
/// 
namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    public class AddressPlugin : AbstractComponentPlugin
    {
        public override ProcessResultsCollection FieldRetrieval(IBusinessComponent component, ProcessInvocationPoint point, ref Field field, int pluginId)
        {
            Address address = component as Address;
            Name name = null;

            if (address.NameToAddress != null && address.NameToAddress.Count > 0)
            {
                name = address.NameToAddress.First().Name as Name;
            }

            if (field.IsInUse == true && field.Visible && name != null)
            {
                if (field.PropertyName == Address.CustomReference04FieldName && name.NameUsages.Count > 0)
                {
                    if (name.NameUsages.OrderBy(n => n.NameUsageID).First().GetDefinitionComponent().CustomCode01 == IDConstants.NAME_CONTROLLED_OUTSIDE_GENIUSX
                        && component.IsEditable)
                    {
                        field.Readonly = false;
                    }
                    else
                    {
                        field.Readonly = true;
                    }
                }
            }

            return null;
        }
    }
}
