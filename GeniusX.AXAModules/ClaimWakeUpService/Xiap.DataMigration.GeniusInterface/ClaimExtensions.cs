using Xiap.Claims.BusinessTransaction;

namespace Xiap.DataMigration.GeniusInterface.AXACS
{
    using System;
    using System.Globalization;
    using Entities;
    using Framework.BusinessTransaction;
    using Gateways;
    using Microsoft.Practices.Unity;

    public static class ClaimExtensions
    {
        private static readonly object TransLock = new object();

        public static AmendClaimWithoutValidationTransaction CreateAmendClaimWithoutValidationTransaction(this Claim c)
        {
            return CreateAmendClaimWithoutValidationTransaction(c, false);
        }

        public static AmendClaimWithoutValidationTransaction CreateAmendClaimWithoutValidationTransaction(this Claim c, bool initialCallFromAttachClaimToPolicyBlock)
        {
            AmendClaimWithoutValidationTransaction amendTransaction;

            var parameters = new[] { c.GeniusXHeaderId.ToString(CultureInfo.InvariantCulture), true.ToString(), String.Empty, false.ToString() };
            lock (TransLock)
            {
                amendTransaction = (AmendClaimWithoutValidationTransaction)BusinessTransactionFactory.GetBusinessTransactionByName("Claims.AmendClaimWithoutValidation", parameters);
            }

            var claimMarkedAsProcessed = amendTransaction.ClaimHeader.CustomCode18 != null;
            
            // We set this to false if this is the first time we're doing this call and CustomCode18 is F00, F01 or F02
            if (initialCallFromAttachClaimToPolicyBlock)
            {
                if (claimMarkedAsProcessed && amendTransaction.ClaimHeader.CustomCode18.StartsWith("F"))
                    claimMarkedAsProcessed = false;
            }
            // Otherwise, this isn't the first time through so we need to ignore a P01 as that's US who set that to say we're processing.
            else
            {
                if (claimMarkedAsProcessed && amendTransaction.ClaimHeader.CustomCode18 == "P01")
                    claimMarkedAsProcessed = false;
            }

            if (claimMarkedAsProcessed)
            {
                amendTransaction.Cancel();
                amendTransaction = null;
                c.ClaimAlreadyProcessed = true;
            }
            else if (c.ClaimDetails == null)
            {
                c.ClaimDetails = GlobalClaimWakeUp.Container.Resolve<Func<string, IStagingGateway>>()(c.ClaimReference).GetClaimDetails(c.ClaimReference);
            }

            return amendTransaction;
        }

        public static AbstractClaimsBusinessTransaction CreateAmendClaimTransaction(this Claim c)
        {
            AbstractClaimsBusinessTransaction amendTransaction;

            var parameters = new[] { c.ClaimReference, true.ToString(), String.Empty, false.ToString() };
            lock (TransLock)
            {
                amendTransaction = (AbstractClaimsBusinessTransaction)BusinessTransactionFactory.GetBusinessTransactionByName("Claims.AmendClaim", parameters);
            }
            // Need to allow P01 work through.
            var claimMarkedAsProcessed = !(amendTransaction.ClaimHeader.CustomCode18 == null || amendTransaction.ClaimHeader.CustomCode18 == "P01");
            if (claimMarkedAsProcessed)
            {
                amendTransaction.Cancel();
                amendTransaction = null;
                c.ClaimAlreadyProcessed = true;
            }
            else if (c.ClaimDetails == null)
            {
                c.ClaimDetails = GlobalClaimWakeUp.Container.Resolve<Func<string, IStagingGateway>>()(c.ClaimReference).GetClaimDetails(c.ClaimReference);
            }

            return amendTransaction;
        }
    }
}
