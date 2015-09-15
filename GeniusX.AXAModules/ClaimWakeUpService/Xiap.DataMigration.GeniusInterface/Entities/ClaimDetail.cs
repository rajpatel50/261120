namespace Xiap.DataMigration.GeniusInterface.AXACS.Entities
{
	public class ClaimDetail
	{
		public decimal? LiabilityIncidentNbr {get; set;} 

		public decimal? LiabilityClaimant {get; set;} 

		public decimal? MotorNOtificationNbr {get; set;} 

		public decimal? MotorTPNo {get; set;}

        public long GeniusXHeaderID { get; set; } 

		public long GeniusXDetailID {get; set;} 

		public string ClaimDetailType {get; set;} 

		public long? GeniusXClaimHandler {get; set;}

		public string ClaimReference {get; set;}

		public string PolicyNumber { get; set; }

        public string CmsPolicySectionCode { get; set; }

		public string PolicySectionCode { get; set; }

	    public string SectionDetailIdentifier { get; set; }

		public string CoverageCode { get; set; }

		public string ProductType { get; set; }
	}
}
