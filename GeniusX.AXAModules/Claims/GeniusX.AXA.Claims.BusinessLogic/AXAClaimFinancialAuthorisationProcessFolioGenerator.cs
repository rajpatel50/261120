using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic;
using Xiap.Framework.Logging;
using Xiap.Framework.Validation;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Generates the string to be used as the K2 Folio—the identifier—for the task. 
    /// This can consist of the ClaimReference, ClaimDetailReference, ClaimTransactionHeaderReference and the Insured List Name.
    /// Invoked when A Claim financial authorisation process is started.
    /// </summary>
    public class AXAClaimFinancialAuthorisationProcessFolioGenerator : IClaimFinancialAuthorisationProcessFolioGenerator
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// This method generates the folio by concatenating the values Claim Reference, Claim Detail Reference and Claim Transaction Header Reference of a task.
        /// </summary>
        /// <param name="claimTransactionHeader">Claim Transaction Header</param>
        /// <returns>Returns folio</returns>
        public string GenerateFolio(ClaimTransactionHeader claimTransactionHeader)
        {
            ArgumentCheck.ArgumentNullCheck(claimTransactionHeader, "claimTransactionHeader");
            string folio = string.Empty;
            ClaimHeader clmHeader = claimTransactionHeader.ClaimHeader;
            string claimTransactionHeaderReference = claimTransactionHeader.ClaimTransactionHeaderReference;
            string claimReference = clmHeader.ClaimReference;

            if (claimTransactionHeader.ClaimTransactionGroups.Count == 1)
            {
                string claimDetailReference = claimTransactionHeader.ClaimTransactionGroups.Single().ClaimDetail.ClaimDetailReference;
                folio = string.Format("{0}-{1} {2}", claimReference, claimDetailReference, claimTransactionHeaderReference);
            }
            else
            {
                folio = string.Format("{0} {1}", claimReference, claimTransactionHeaderReference);
            }

           ClaimNameInvolvement claimNameInvolvement = ClaimsBusinessLogicHelper.GetInsuredFromHeader(clmHeader);
           folio = this.GenerateFolio(folio, claimNameInvolvement);
           if (_Logger.IsDebugEnabled)
           {
               _Logger.Debug(string.Format("Generated Folio is :", folio));
           }

           return folio;
        }

        /// <summary>
        /// Returns the folio by concatenation the list name of insured name involvement with folio.
        /// </summary>
        /// <param name="folio">Concatenated Folio</param>
        /// <param name="claimNameInvolvement">Claim Name Involvement</param>
        /// <returns>Returns list name</returns>
        private string GenerateFolio(string folio, ClaimNameInvolvement claimNameInvolvement)
        {
            string listName = string.Empty;
            if (claimNameInvolvement != null && claimNameInvolvement.NameID.HasValue)
            {
                listName = ClaimsBusinessLogicHelper.GetListName(claimNameInvolvement.NameID.Value);
            }

            return this.GenerateFolio(folio, listName);
        }

        /// <summary>
        /// Suffix insured list name.
        /// </summary>
        /// <param name="folio">Concatenated folio</param>
        /// <param name="insuredListName">Insured ListName</param>
        /// <returns>Folio with Concatenated insured list name</returns>
        private string GenerateFolio(string folio, string insuredListName)
        {
            if (!string.IsNullOrEmpty(insuredListName))
            {
                return folio + "/" + insuredListName;
            }
            else
            {
                return folio;
            }
        }
    }
}
