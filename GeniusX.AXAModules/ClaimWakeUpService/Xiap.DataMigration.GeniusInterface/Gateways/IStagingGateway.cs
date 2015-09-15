namespace Xiap.DataMigration.GeniusInterface.AXACS.Gateways
{
    using System.Collections.Generic;
    using Entities;

    public interface IStagingGateway
    {
        int GetClaimByPolicyCount(bool includeOpenClaims, bool includeClosedClaims, bool includeLiability, bool includeMotor, string[] policiesToInclude, string[] policiesToExclude);
        int GetClaimByClaimCount(bool includeOpenClaims, bool includeClosedClaims, bool includeLiability, bool includeMotor, string[] claimsToInclude, string[] claimsToExclude);
        IEnumerable<Claim> GetClaimsByPolicy(int size, int firstRecord, bool includeOpenClaims, bool includeClosedClaims, bool includeLiability, bool includeMotor, string[] policiesToInclude, string[] policiesToExclude);
        IEnumerable<Claim> GetClaimsByClaim(int size, int firstRecord, bool includeOpenClaims, bool includeClosedClaims, bool includeLiability, bool includeMotor, string[] claimsToInclude, string[] claimsToExclude);
        IEnumerable<ClaimDetail> GetClaimDetails(string claimReference);
        IEnumerable<string> GetPolicyReferences();

    }
}