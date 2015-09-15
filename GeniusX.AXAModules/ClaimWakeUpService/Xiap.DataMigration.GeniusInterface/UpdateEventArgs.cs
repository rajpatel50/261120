using System;
using System.Collections.Generic;

namespace Xiap.DataMigration.GeniusInterface.AXACS
{
    using Xiap.DataMigration.GeniusInterface.AXACS.Entities;

    public class UpdateEventArgs : EventArgs
    {
        public int TotalClaims { get; set; }

        public int BatchSize { get; set; }

        public bool ClaimProcessed { get; set; }

        public bool ClaimAlreadyProcessed { get; set; }

        public bool ClaimFailed { get; set; }

        public bool ClaimTransferred { get; set; }

        public bool ClaimIsDuplicate { get; set; }

        public int SkippedClaims { get; set; }
        
        public IEnumerable<ClaimReferenceCount> ClaimReferenceCounts { get; set; }    
    }
}
