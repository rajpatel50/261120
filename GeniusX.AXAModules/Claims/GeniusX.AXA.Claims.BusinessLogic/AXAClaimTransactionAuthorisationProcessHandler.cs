using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic;
using Xiap.Framework.Logging;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class AXAClaimTransactionAuthorisationProcessHandler : ClaimTransactionAuthorisationProcessHandler
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Generates the data fields from base and assign the value of 'Final Settlment?'(CustomBoolean01 of ClaimPaymentRequest) to TaskOnClaimFinalSettlement field.
        /// </summary>
        /// <param name="claimTransactionheader">Claim Transaction Header</param>
        /// <param name="authorisationMode">Authorisation Mode</param>
        /// <returns>Generic Data Fields as a Dictionary Object</returns>
        protected override Dictionary<string, object> GenerateDataFields(Xiap.Claims.BusinessComponent.ClaimTransactionHeader claimTransactionheader, Xiap.Claims.BusinessLogic.ClaimsBusinessLogicHelper.AuthorisationMode authorisationMode)
        {
            var dataFields = base.GenerateDataFields(claimTransactionheader, authorisationMode);

            // If we're authorising a Payment
            if (authorisationMode == Xiap.Claims.BusinessLogic.ClaimsBusinessLogicHelper.AuthorisationMode.Payment)
            {
                // Check the ClaimTransactionHeader we're authorising a payment on has some links to Payment Requests
                if (claimTransactionheader.ClaimTransactionToPaymentRequestLinks != null)
                {
                    // If it has some payment requests it links to, find the first, current one.
                    ClaimTransactionToPaymentRequestLink paymentLink = claimTransactionheader.ClaimTransactionToPaymentRequestLinks.Where(o => o.IsLatest == true).FirstOrDefault();
                    // If we have a link to a payment request and it's for a Claim Payment Request, then process.
                    if (paymentLink != null && paymentLink.ClaimPaymentRequest != null)
                    {
                        dataFields[ClaimConstants.DATAFIELD_TASKONCLAIMFINALSETTLEMENT] = paymentLink.ClaimPaymentRequest.CustomBoolean01.GetValueOrDefault(false);   // UI Label = Final Settlement For Claim?
                        logger.Info("dataFields[TaskOnClaimFinalSettlement] : " + dataFields["TaskOnClaimFinalSettlement"]);
                    }
                }
            }

            return dataFields;
        }
    }
}
