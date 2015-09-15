namespace Xiap.DataMigration.GeniusInterface.AXACS.Entities
{
    using System;
    using System.Collections.Generic;

    public class Claim
    {
        public string CustomCode18 { get; set; }

        public bool ClaimProcessingFailed { get; set; }

        public string FailureReason { get; set; }

        public bool ClaimAlreadyProcessed { get; set; }

        public bool ClaimProcessingCompleted { get; set; }

        public bool ClaimTransfered { get; set; }

        public bool ClaimIsDuplicate { get; set; }

        public bool ClaimIsClosed { get; set; }

        public bool ClaimIsAttachedToPolicy { get; set; }

        public bool ExcessAndDeductiblesToProcess { get; set; }

        public bool PolicyShellWasCreated { get; set; }

        public string PolicyHeaderStatusCode { get; set; }

        // Stuff that comes back from the query.
        
        public string ClaimReference { get; set; }

        public long GeniusXHeaderId { get; set; }

        public long GeniusXClaimHandler { get; set; }

        public string PolicyNumber { get; set; }

        public string ProductType { get; set; }

        public IEnumerable<ClaimDetail> ClaimDetails { get; set; }
    }
}
