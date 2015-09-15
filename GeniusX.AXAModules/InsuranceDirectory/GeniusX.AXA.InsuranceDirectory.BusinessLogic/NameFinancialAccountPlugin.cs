using System.Linq;
using Xiap.Framework;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;


namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    /// <summary>
    /// If the Name is maintained outside of Xuber, this sets CustomRefernce04 on the NameFinancialAccount—which holds the
    /// ID of the NameFinancialAccount in the external system—to be editable so a user can enter a value.
    /// </summary>
    public class NameFinancialAccountPlugin : AbstractComponentPlugin
    {
        public override ProcessResultsCollection FieldRetrieval(IBusinessComponent component, ProcessInvocationPoint point, ref Field field, int pluginId)
        {
            NameFinancialAccount nfa = component as NameFinancialAccount;
            Name name = nfa.Name as Name;

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