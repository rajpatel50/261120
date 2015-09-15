using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common.Product;

namespace GeniusX.AXA.Claims.BusinessLogic
{
   public class ClaimHandlerEventDefaulterPlugin : ITransactionPlugin
    {
       /// <summary>
       /// Call on ClaimHandlerEventDefaulter.
       /// Generate an event if this is a PreComplete invocation point.
       /// </summary>
       /// <param name="businessTransaction">Claim Header</param>
       /// <param name="point">Point PreComplete</param>
       /// <param name="PlunginId">PlugIn ID</param>
       /// <param name="Paramaters">Processing Transaction Parameters</param>
       /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public ProcessResultsCollection ProcessTransaction(IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PlunginId, params object[] Paramaters)
        {
            ClaimHeader claimHeader = (ClaimHeader)businessTransaction.Component;
            if (point == TransactionInvocationPoint.PreComplete)
            {
                this.GenerateEvent(claimHeader);
            }

            return businessTransaction.Results;
        }

       /// <summary>
       /// Add new event on ClaimHeader if there are ClaimInvolvements on an Amend Claim or Change Claim Handler
       /// where the claim handler has been modified.
       /// </summary>
       /// <param name="claimHeader">Claim Header</param>
        private void GenerateEvent(ClaimHeader claimHeader)
        {
            if (claimHeader.ClaimInvolvements == null)
            {
                return;
            }

            string transactionType = claimHeader.Context.TransactionType;
            if (transactionType != ClaimConstants.TRANSACTION_TYPE_AMEND_CLAIM && transactionType != ClaimConstants.TRANSACTION_TYPE_CHANGE_CLAIM_HANDLER)
            {
                return;
            }

            // Has the Claim Handler been changed.
            if (claimHeader.ClaimInvolvements.Any(a => this.IsMainClaimHandlerModified(a)))
            {
                var productEvents = ProductService.GetProductEventQuery().GetProductEvents(claimHeader.ProductVersionID.GetValueOrDefault());

                // If this product has an event of the system update type defined, raise it against the claim handler, to show it's been updated.
                if (productEvents.Any(x => x.EventTypeCode == ClaimConstants.SYSUPD_Event_Type))
                {
                    var productEvent = productEvents.Where(x => x.EventTypeCode == ClaimConstants.SYSUPD_Event_Type).First();
                    ClaimEvent claimEvent = claimHeader.AddNewClaimEvent(productEvent.ProductEventID, true);
                    claimEvent.EventDescription = ClaimConstants.CLAIM_HANDLER_CHANGED;
                }
            }
        }

       /// <summary>
       /// Check if the ClaimHandler is a new name.
       /// </summary>
       /// <param name="claimInvolvement">Claim Involvement</param>
       /// <returns>return true if ClaimHandler is modified</returns>
        private bool IsMainClaimHandlerModified(ClaimInvolvement claimInvolvement)
        {
            return claimInvolvement.ClaimNameInvolvements.Any(a => a.NameInvolvementType == (short)StaticValues.NameInvolvementType.MainClaimHandler && a.IsNew == true);
        }
    }
}
