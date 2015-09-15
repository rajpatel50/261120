using System.Linq;
using Xiap.Framework.Common;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.InsuranceDirectory.BusinessLogic;

namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    public class AXANameTransferValidation : INameTransferValidation
    {
        public bool ValidateNameForTransfer(string nameReference)
        {
            bool validNameUsage = true;
            if (nameReference != null)
            {
                InsuranceDirectoryEntities entities = InsuranceDirectoryEntitiesFactory.GetInsuranceDirectoryEntities();
                Name name = (from result in entities.Name where result.NameReference == nameReference select result).FirstOrDefault();
                if (name != null)
                {
                    foreach (NameUsage usage in name.NameUsages)
                    {
                        ////Check the value from the decision table configured in Genius Configuration NTNameUsageDecisionTable or 'X-NTU'                
                        if (InsuranceDirectoryHelper.IsNameUsagevalidForTransfer(usage) == false)
                        {
                            validNameUsage = false;
                            break;
                        }
                    }
                }
            }

            return validNameUsage;
        }
    }
}
