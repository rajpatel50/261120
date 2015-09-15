using System.Collections.Generic;
using Xiap.Framework.Metadata;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Stores the Deductible Reason Codes dictionary within a data class
    /// for passing through the claim contexts.
    /// </summary>
    public class DeductibleReasonCode
    {
        public DeductibleReasonCode(Dictionary<string, List<CodeRow>> policyReasonCodes)
        {
            this.PolicyReasonCodes = policyReasonCodes;
        }

        public Dictionary<string, List<CodeRow>> PolicyReasonCodes { get; set; }
    }
}
