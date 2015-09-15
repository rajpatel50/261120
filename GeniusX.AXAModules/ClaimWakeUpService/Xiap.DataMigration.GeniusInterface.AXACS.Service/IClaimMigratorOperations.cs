using System.ServiceModel;
using System.ServiceModel.Web;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IClaimMigratorOperations
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "", Method = "POST")]
        void ProcessClaim(string ClaimReference);
    }

}
