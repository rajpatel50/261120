namespace Xiap.DataMigration.GeniusInterface.AXACS.Gateways
{
    using System.Collections.Generic;
    using System.Data.SqlClient;

    using Dapper;

    using Xiap.DataMigration.GeniusInterface.AXACS.Entities;

    public class GeniusXGateway : IGeniusXGateway
    {
        private readonly string _connectionString;

        #region queries
        #endregion
        private const string DuplicateClaimCheckQuery = 
@"SELECT ClaimReference, count(ClaimHeaderID) as ReferenceCount 
FROM Claims.ClaimHeader 
WHERE CustomCode19 = 'C'
GROUP BY ClaimReference";

        private const string ProductEventQuery = @"select ProductCode, EventTypeCode, ProductEventID
from Code.ProductVersion A
join Code.ProductEvent B on A.productversionid = B.productversionid";


        public GeniusXGateway(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<ProductEvent> GetProductEventIDs()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                return connection.Query<ProductEvent>(ProductEventQuery);
            }
        }

        public IEnumerable<ClaimReferenceCount> GetClaimReferenceCounts()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                return connection.Query<ClaimReferenceCount>(DuplicateClaimCheckQuery);
            }
        }
    }
}
