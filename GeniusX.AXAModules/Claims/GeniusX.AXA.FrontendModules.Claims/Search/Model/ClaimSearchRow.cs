using System;
using XIAP.FrontendModules.Claims.Search.Model;
using XIAP.FrontendModules.Common.SearchService;

namespace GeniusX.AXA.FrontendModules.Claims.Search.Model
{
    /// <summary>
    /// Class that will hold the duplicate claims search data.
    /// </summary>
    public class AXAClaimSearchRow : ClaimSearchRow
    {
        public AXAClaimSearchRow(SearchResultRow row)
            : base(row)
        {
        }

        public override string RowKey
        {
            get { return Convert.ToString(this.ClaimReference); }
        }

        public new string ClaimReference
        {
            get { return this.GetColumnValue<string>("ClaimReference"); }
        }

        public new string ClaimTitle
        {
            get { return this.GetColumnValue<string>("ClaimTitle"); }
        }

        public new string Claimant
        {
            get { return this.GetColumnValue<string>("Claimant"); }
        }

        public new DateTime? DateOfLossFrom
        {
            get
            {
                return this.GetColumnValue<DateTime>("DateOfLossFrom");
            }
        }

        public new string Insured
        {
            get { return this.GetColumnValue<string>("Insured"); }
        }

        public string TPRegistrationNumber
        {
            get { return this.GetColumnValue<string>("TPRegistrationNumber"); }
        }

        public string Driver
        {
            get { return this.GetColumnValue<string>("Driver"); }
        }

        public string RegistrationNumber
        {
            get { return this.GetColumnValue<string>("RegistrationNumber"); }
        }

        public string ClientReference
        {
            get { return this.GetColumnValue<string>("ClientReference"); }
        }

        public string OutsourceReference
        {
            get { return this.GetColumnValue<string>("OutsourceReference"); }
        }

        public int TotalClaimsCount
        {
            get { return this.GetColumnValue<int>("TotalClaimsCount"); }
        }

        public new string ViewUniqueId
        {
            get;
            set;
        }

        public override string ViewId
        {
            get
            {
                return this.ViewUniqueId;
            }
        }
    }
}
