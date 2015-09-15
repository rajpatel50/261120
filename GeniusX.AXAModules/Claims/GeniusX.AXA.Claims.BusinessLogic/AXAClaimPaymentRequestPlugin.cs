using Xiap.Framework.ProcessHandling;
using Xiap.Framework;
using Xiap.Claims.BusinessComponent;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class AXAClaimPaymentRequestPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Set the PaymentRequestReference to the Reference (CustomReference01) value from the NameInvolvement for the selected Addressee Name
        /// </summary>
        /// <param name="component">Claim Payment Request component</param>
        /// <param name="point">Process Point</param>
        /// <param name="propertyName">Property Name, in this case AddresseeNameID</param>
        /// <param name="oldValue">Old Value, current value of the property on the PaymentRequest</param>
        /// <param name="newValue">New Value, newly selected value to apply to the Property</param>
        /// <param name="pluginId">Plugin Id</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection PropertyChange(IBusinessComponent component, ProcessInvocationPoint point, string propertyName, object oldValue, object newValue, int pluginId)
        {
            ClaimPaymentRequest claimPaymentRequest = (ClaimPaymentRequest)component;
            ClaimNameInvolvement selectedAddersseeNameReference = claimPaymentRequest.ClaimTransactionToPaymentRequestLink.ClaimTransactionHeader.AddresseeClaimNameInvolvement;

            // If we are changing the property on the AddresseeNameID field
            // and the supplied new value isn't the same as the old value and isn't null, process the new value.
            if (propertyName == ClaimPaymentRequest.AddresseeNameIDFieldName && oldValue != newValue && newValue != null)
            {
                // We make absolutely sure the Claim Payment Request we are updating is linked to a valid ClaimTransactionHeader
                // and that we have an AddresseeNameReference, from the Addressee NI on the Claim to use.
                if (claimPaymentRequest.ClaimTransactionToPaymentRequestLink != null &&
                    claimPaymentRequest.ClaimTransactionToPaymentRequestLink.ClaimTransactionHeader != null &&
                    selectedAddersseeNameReference != null)
                {
                    // Apply CustomReference01 to the PaymentRequest Reference.
                    claimPaymentRequest.PaymentRequestReference = selectedAddersseeNameReference.CustomReference01;
                }
            }
            
            return null;
        }
    }
}
