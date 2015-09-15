using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Claims.BusinessLogic;
using Xiap.Framework.Locking;
using Xiap.Metadata.Data.Enums;
using Xiap.Metadata.BusinessComponent;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class AXAClaimDetailZeroReserveTransactionPlugin : ITransactionPlugin
    {
        /// <summary>
        /// This method sets the value of Reason (ClaimTransactionDescription) as "Claim Detail Closed" and allocates next transaction reference to Claim Transaction Reference (ClaimTransactionHeaderReference) 
        /// if claim detail is being closed and corresponding reserves are being set to zero.  
        /// </summary>
        /// <param name="businessTransaction">Business Transaction</param>
        /// <param name="point">Transaction Point</param>
        /// <param name="PluginId">Plugin Id</param>
        /// <param name="parameters">Extra Parameters</param>
        /// <returns>Process Results Collection</returns>
        public ProcessResultsCollection ProcessTransaction(Xiap.Framework.BusinessTransaction.IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            if (point == TransactionInvocationPoint.PreComplete)
            {
                ClaimHeader claimHeader = (ClaimHeader)businessTransaction.Component;
                foreach (var inProgressTransactionHeader in claimHeader.InProgressClaimTransactionHeaders)
                {
                    if (inProgressTransactionHeader.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Reserve || inProgressTransactionHeader.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReserve)
                    {
                        foreach (var ctg in inProgressTransactionHeader.ClaimTransactionGroups)
                        {
                            ClaimDetail claimDetail = ctg.ClaimDetail;
                            if (claimDetail.OriginalTransitionStatus != claimDetail.ClaimDetailStatusCode)
                            {
                                // if ClaimDetail is being closed or finalized
                                if (claimDetail.ClaimDetailInternalStatus == (short?)StaticValues.ClaimDetailInternalStatus.ClosedCreatedinError ||
                                    claimDetail.ClaimDetailInternalStatus == (short?)StaticValues.ClaimDetailInternalStatus.Finalized)
                                {
                                    // if finalization is allowed with non-zero Outstanding
                                    if (claimDetail.GetProduct().IsFinalizationWithNonZeroOSAllowed.GetValueOrDefault(false))
                                    {
                                        inProgressTransactionHeader.ClaimTransactionDescription = "Claim Detail Closed";
                                        inProgressTransactionHeader.ClaimTransactionHeaderReference = LockManager.AllocateReference(string.Empty, ReferenceType.ClaimTransactionHeaderReference, string.Empty, "0000000001", 10, "9999999999", false);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return businessTransaction.Results;
        }
    }
}
