using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Data class to store the Inactive Claim Detail data.
    /// </summary>
    public class InactivityClaimDetail
    {
        public long ClaimHeaderID { get; set; }
        public long ClaimDetailID { get; set; }
        public string CustomCode20 { get; set; }
        public string ClaimReference { get; set; }
        public long? NameID { get; set; }
        public string ClaimDetailReference { get; set; }
        public int ClaimTransactionSource{ get; set;}
    }
}
