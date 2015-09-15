namespace Xiap.DataMigration.GeniusInterface.AXACS.Gateways
{
    using System.Collections.Generic;
    using Entities;

    public interface IGeniusXGateway
    {
        IEnumerable<ProductEvent> GetProductEventIDs();
        IEnumerable<ClaimReferenceCount> GetClaimReferenceCounts();
    }
}