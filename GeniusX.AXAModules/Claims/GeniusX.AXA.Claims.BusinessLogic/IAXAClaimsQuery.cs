using System.Collections.Generic;
using Xiap.Framework.Common;
using Xiap.Framework.Data.Claims;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.Metadata;
using Xiap.Metadata.Data.Enums;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public interface IAXAClaimsQuery
    {
        Header GetPolicyHeaderByHeaderID(long headerID);
        string GetHeaderStatus(string headerReference);
        long GetHeaderID(string externalReference);
        long GetSectionID(string externalReference);
        long GetSectionDetailID(string externalReference);
        long GetCoverageID(string externalReference);
        void PolicyDeductiblesExist(long policyHeaderID, string identifier1, string identifier2, out bool deductibleExist, out bool EDExcessExist);
        void ExecuteClaimTransferControlLogSP(string claimReference);
        IEnumerable<IGenericDataItem> GetPolicyDeductibles(long componentID, short linkLevel, string vehicleType, string division, string reasonCode, string identifier);
        Dictionary<string, List<CodeRow>> GetPolicyReasonCodes(string headerReference, string identifier);
        string[] GetPolicyReferences(string headerReference);
	    List<ClaimFinancialAmount> ExecuteAccumulatedClaimAmounts(long claimDetailId,long claimTransactionHeaderId, StaticValues.AmountType amountType);
        IUWTerms GetlatestTerm(long headerID);
        bool HasFinancialTransactionInTheClaim(long claimHeaderId);
        bool IsUserOutOfOffice(long userId, out string taskRedirectUserIdentity);
    }
}
