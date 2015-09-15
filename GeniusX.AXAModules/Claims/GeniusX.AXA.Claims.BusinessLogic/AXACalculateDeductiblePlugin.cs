using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class AXACalculateDeductiblePlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Sets the Reserve Re-statement Required? Setting on the Claim Transaction Header to true if one of the following
        /// errors—that require the reserve to be manually reviewed—are thrown during Deductible processing:
        /// - RecoveryReservesRequireManualReview 
        /// - ReservesRequireManualReview
        /// <para/>
        /// This methods sets the value of ClaimTransactionHeader field 'Rsv Re-statmnt Rqd?' (CustomBoolean04) to true 
        /// <para/>
        /// if RecoveryReservesRequireManualReview or ReservesRequireManualReview message has been added.
        /// </summary>
        /// <param name="component">Transactionheader Component</param>
        /// <param name="point">Claim Transaction Header</param>
        /// <param name="pluginId">Plugin Id</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            ClaimTransactionHeader claimTransHeader = component as ClaimTransactionHeader;
            if (point == ProcessInvocationPoint.Virtual  && claimTransHeader != null && component.Context != null)
            {
                var result = component.Context.Results.Results.Where(a => (a.MessageId == MessageConstants.RecoveryReservesRequireManualReview || a.MessageId == MessageConstants.ReservesRequireManualReview)).FirstOrDefault();
                if (result != null)
                {
                    claimTransHeader.CustomBoolean04 = true;   // UI Label = Reserve Re-statement Required?
                }
                else
                {
                    claimTransHeader.CustomBoolean04 = false;   // UI Label = Reserve Re-statement Required?
                }
            }

            return null;
        }
    }
}
