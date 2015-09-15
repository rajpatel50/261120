using System.Linq;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;

  
namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    /// <summary>
    /// Verifies permissions to manage non-Xuber sourced Names, for example, those from Genius.
    /// </summary>
    public class VerifyPermissionForName : ITransactionPlugin
    {
        /// <summary>
        /// Process on Pre-Create of a Genius controlled name usage type.
        /// </summary>
        /// <param name="businessTransaction">The business transaction.</param>
        /// <param name="point">The point.</param>
        /// <param name="PluginId">The plugin identifier.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Process Results Collection</returns>
        public ProcessResultsCollection ProcessTransaction(IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            if (point == TransactionInvocationPoint.PreCreate)
            {
                Name sourceName = businessTransaction.Component as Name;
                if (sourceName.NameUsages.Any(nu=>nu.GetDefinitionComponent().CustomCode01 != IDConstants.NAME_CONTROLLED_IN_GENIUSX))
                {
                    // This will throw an exception of the user isn't allowed to maintain Genius-controlled names.
                    InsuranceDirectoryBusinessLogicHelper.VerifyPermissionForCurrentUser(IDConstants.GENIUS_SOURCED_NAME_MAINTENANCE_PERMISSION_TOKEN);
                }
            }

            return businessTransaction.Results;
        }
    }
}
