using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Logging;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common.Product;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ClaimEventOnCancelPaymentTransactionPlugin : ITransactionPlugin
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Call on CancelPaymentTransaction 
        /// </summary>
        /// <param name="businessTransaction">Claim Transaction</param>
        /// <param name="point">Point PostCreate</param>
        /// <param name="PluginId">Plugin ID</param>
        /// <param name="parameters">Processing Transaction Parameters</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public ProcessResultsCollection ProcessTransaction(Xiap.Framework.BusinessTransaction.IBusinessTransaction businessTransaction, Xiap.Framework.TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            if (point == TransactionInvocationPoint.PostCreate)
            {
                this.CreateEventOnTransaction(businessTransaction);
            }

            return businessTransaction.Results;
        }

        /// <summary>
        /// Create a Payment Cancellation Event against the claim detail for this Claim Transaction, if applicable.
        /// </summary>
        /// <param name="businessTransaction">Claim Header</param>
        private void CreateEventOnTransaction(IBusinessTransaction businessTransaction)
        {
            ClaimHeader clmHeader = null;

            clmHeader = businessTransaction.Component.Parent as ClaimHeader;
            
            if (clmHeader == null)
            {
                // If there's no Claim Header associated with this transaction do no further processing.
                return;
            }

            // Check if there are Claim Transaction Headers in progress for this claim header that we can process against.
            if (clmHeader.InProgressClaimTransactionHeaders != null)
            {
                // Find a Product Event for this Product Version for a Payment Cancellation
                var productEvent = ProductService.GetProductEventQuery().GetProductEvents(clmHeader.ProductVersionID.GetValueOrDefault())
                            .Where(x => x.EventTypeCode == ClaimConstants.EVENT_TYPECODE_PAYMENTCANCELLATION).FirstOrDefault();

                if (productEvent != null)
                {
                    // We have a Product Event that can be raised so cycle through all the In Progress CTHs
                    foreach (ClaimTransactionHeader clmTransHeader in clmHeader.InProgressClaimTransactionHeaders)
                    {
                        if (clmTransHeader.ClaimTransactionGroups != null)
                        {
                            // We need at least one claim transaction group on the CT header, although we only use the first.
                            ClaimTransactionGroup clmTransGroup = clmTransHeader.ClaimTransactionGroups.First();
                            // Only work with Payment Cancellation Claim Transaction Header types.
                            switch (clmTransHeader.ClaimTransactionSource)
                            {
                                case (short)StaticValues.ClaimTransactionSource.PaymentCancellation:
                                    // Find out if we have a Payment amount against any Claim Transaction Details on the current CT Group for this CT Header
                                    bool result = clmTransGroup.ClaimTransactionDetails.AsEnumerable<ClaimTransactionDetail>().Any(x => x.AmountType == (short)StaticValues.AmountType.Payment);
                                    if (result)
                                    {
                                        // If so, raise the Payment Cancellation event against the claim detail for this group.
                                        this.CreateEvent(clmTransGroup.ClaimDetail, clmTransHeader, productEvent.ProductEventID);
                                    }

                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create Event of type Cancel Payment against the Claim Detail
        /// </summary>
        /// <param name="clmDetail">Claim Detail</param>
        /// <param name="clmTransHeader">Claim Transaction Header</param>
        /// <param name="productEventID">Product EventID</param>
        private void CreateEvent(ClaimDetail clmDetail, ClaimTransactionHeader clmTransHeader, long productEventID)
        {
            if (clmDetail != null)
            {
              ClaimEvent claimEvent = clmDetail.AddNewClaimEvent(productEventID, true);
              // If we are using CustomText01 on the Claim Event
              // UI Label = Cancellation Reason; Payment Cancelled Task
              if (clmTransHeader != null && claimEvent.CustomText01Field.IsInUse == true)
              {
                  // Put the cancellation reason in ot CustomText01
                  // CustomText01 = Cancellation Reason
                   claimEvent.CustomText01 = clmTransHeader.ClaimTransactionDescription;   // UI Label = Cancellation Reason; Payment Cancelled Task
              }
            }
        }
    }
}
