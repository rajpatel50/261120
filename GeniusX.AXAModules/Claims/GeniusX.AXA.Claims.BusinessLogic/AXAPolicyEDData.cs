using System.Collections.Generic;
using Xiap.Framework.Metadata;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// This class represents two Boolean data values for Policy Data and therefore
    /// allows you to default them automatically.
    /// The values are saying: Do deductibles exist on the policy?
    /// Does Excess Deducible Exess exist on the policy?
    /// </summary>
    public class AXAPolicyEDData
    {
        public AXAPolicyEDData(bool deductiblesExist, bool excessDedExcessExist)
        {
            this.DeductiblesExist = deductiblesExist;
            this.EDExcessExist = excessDedExcessExist;
        }

        public bool DeductiblesExist { get; set; }
        public bool EDExcessExist { get; set; }
    }
}
