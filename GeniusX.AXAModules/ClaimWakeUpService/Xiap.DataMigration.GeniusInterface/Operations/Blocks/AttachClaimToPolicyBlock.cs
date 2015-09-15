namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Claims.BusinessComponent;
    using Entities;
    using Framework.Locking;
    using Handlers;
    using Metadata.Data.Enums;
    using Newtonsoft.Json.Linq;
    using Xiap.UW.BusinessComponent;
    using log4net;
    using Microsoft.Practices.Unity;

    [BlockDescription("Policy Attachment", Description="Attaches the Claim to the Policy, sets the Date Of Loss type code.")]
    public class AttachClaimToPolicyBlock : IBlock
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AttachClaimToPolicyBlock));

        public static Claim Execute(Claim claim)
        {
            var sw = Stopwatch.StartNew();
            if (!ShouldExecute(claim)) return claim;
            // The change here is to allow us to automatically reprocess items that are F00, F01 or F02 as part of the
            // automated system.
            var transaction = claim.CreateAmendClaimWithoutValidationTransaction(true);
            if (transaction == null) return claim;
            try
            {

                var claimHeader = transaction.ClaimHeader;
                claimHeader.DateLastSeen = DateTime.UtcNow;
                // Update Custom Code 18 to NULL if it's an F- code since we now auto-retry those.
                //if (claimHeader.CustomCode18 != null && claimHeader.CustomCode18.StartsWith("F"))
                   // claimHeader.CustomCode18 = null;
                //Update CustomCode18 to be P01 - in progress
                claimHeader.CustomCode18 = "P01";
                if (GetPolicyAndAttachClaim(claim, claimHeader))
                {
                    Logger.Info("Checking for AD ClaimDetail");
                    var adClaimDetail = claimHeader.ClaimDetails.FirstOrDefault(cd => cd.ClaimDetailTypeCode == "AD");
                    if (adClaimDetail != null)
                    {
                        
                        // If there is no Excess on the ClaimDetail then copy it from the ClaimHeader
                        if (!adClaimDetail.PolicyDeductible01.HasValue)
                        {
                            if (adClaimDetail.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod == 
                                (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible
                                 && adClaimDetail.IsAutomaticDeductibleProcessingApplied == true)
                            {
                                Logger.InfoFormat("AD ClaimDetail found and has no Excess so copying from ClaimHeader:\r\n{0}", JObject.FromObject(
                                    new
                                    {
                                        claim.ClaimReference,
                                        adClaimDetail.ClaimDetailReference,
                                        IsDeductible01PaidByInsurer = claimHeader.CustomBoolean15,
                                        PolicyDeductible01 = claimHeader.CustomNumeric10
                                    }));
                                adClaimDetail.IsDeductible01PaidByInsurer = claimHeader.CustomBoolean15;
                                adClaimDetail.PolicyDeductible01 = claimHeader.CustomNumeric10;
                            }
                        }
                        // If there is Excess on the ClaimDetail then assume that is an override and update the ClaimHeader
                        else if(adClaimDetail.PolicyDeductible01 != claimHeader.CustomNumeric10)
                        {
                            Logger.InfoFormat("AD ClaimDetail found but looks like there is an override so updating ClaimHeader:\r\n{0}", JObject.FromObject(
                                   new
                                   {
                                       claim.ClaimReference,
                                       adClaimDetail.ClaimDetailReference, 
                                       adClaimDetail.IsDeductible01PaidByInsurer,
                                       adClaimDetail.PolicyDeductible01
                                   }));
                            claimHeader.CustomBoolean15 = adClaimDetail.IsDeductible01PaidByInsurer;
                            claimHeader.CustomNumeric10 = adClaimDetail.PolicyDeductible01;
                        }
                    }
                    transaction.Complete(false);
                    var policyReferenceLocks = LockManager.CheckLock(claim.PolicyNumber, LockLevel.HeaderReference, LockDurationType.Transaction, LockType.Update, LockOrigin.Underwriting);
                    if (policyReferenceLocks.Any())
                    {
                        foreach (var @lock in policyReferenceLocks)
                        {
                            LockManager.RemoveLock(@lock.LockLevel, @lock.LockData, @lock.TransactionID);
                            Logger.InfoFormat("Remove lock\r\n{0}\r\n", JObject.FromObject(new { claim.PolicyNumber }));
                        }
                    }

                    if (!string.Equals(claim.PolicyHeaderStatusCode, "OPV", StringComparison.OrdinalIgnoreCase))
                    {

                        using (var uwe = new UnderwritingEntities())
                        {
                            var policy = (from h in uwe.Header
                                          where h.HeaderReference == claim.PolicyNumber
                                          select h).FirstOrDefault();
                            if (policy != null)
                            {
                                policy.HeaderStatusCode = "OPV";
                            }
                            uwe.SaveChanges();
                            Logger.InfoFormat("Policy HeaderStatusCode change\r\n{0}\r\n", JObject.FromObject(new { claim.PolicyNumber, policy.HeaderStatusCode }));
                        }

                    }
                }
                else
                {
                    Logger.InfoFormat("Failed, cancelling");
                    transaction.Cancel();
                }
                return claim;
            }
            catch
            {
                transaction.Cancel();
                return claim;
            }
            finally
            {
                sw.Stop();
                var workDone = GlobalClaimWakeUp.Statistics.GetOrAdd(typeof(AttachClaimToPolicyBlock).Name, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                GlobalClaimWakeUp.Statistics[typeof(AttachClaimToPolicyBlock).Name] = (workDone + TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
        }

        private static bool ShouldExecute(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            return config.TaskIsEnabled[typeof(AttachClaimToPolicyBlock)] && (!claim.ClaimIsDuplicate && !claim.ClaimAlreadyProcessed);
        }

        private static bool GetPolicyAndAttachToClaim(ClaimHeader claimHeader, Claim claim)
        {
            var policyAttachmentHandler = GlobalClaimWakeUp.Container.Resolve<AttachClaimToPolicyHandler>();

            var result = policyAttachmentHandler.Execute(claimHeader, claim);

            GlobalClaimWakeUp.GeniusXPolicyState.TryAdd(claim.PolicyNumber, result);

            return result;
        }

        private static bool GetPolicyAndAttachClaim(Claim claim, ClaimHeader claimHeader)
        {
            var headerStatusMap = new Dictionary<string, StaticValues.ClaimHeaderInternalStatus>
                                      {
                                          {"CAB", StaticValues.ClaimHeaderInternalStatus.Finalized},
                                          {"CCL", StaticValues.ClaimHeaderInternalStatus.Finalized},
                                          {"CES", StaticValues.ClaimHeaderInternalStatus.InProgress},
                                          {"CON", StaticValues.ClaimHeaderInternalStatus.InProgress},
                                          {"COU", StaticValues.ClaimHeaderInternalStatus.InProgress},
                                          {"CPY", StaticValues.ClaimHeaderInternalStatus.InProgress},
                                          {"CRE", StaticValues.ClaimHeaderInternalStatus.InProgress},
                                          {"CRL", StaticValues.ClaimHeaderInternalStatus.Finalized},
                                          {"CRO", StaticValues.ClaimHeaderInternalStatus.InProgress},
                                      };
            if (!GetPolicyAndAttachToClaim(claimHeader, claim))
            {
                claim.ClaimProcessingFailed = true;
                claim.FailureReason = "Policy does not exist or could not attach";
                claim.CustomCode18 = "F02";
                return false;
            }
            claimHeader.ClaimHeaderInternalStatus = (short)headerStatusMap[claimHeader.ClaimHeaderStatusCode];
            claim.ClaimIsAttachedToPolicy = true;
            claim.ClaimIsClosed = claimHeader.ClaimHeaderInternalStatus >= 4;

            var updateEdDataPlugin = new UpdateExcessAndDeductibles();
            var claimHeaderInteralStatus = new Dictionary<long, StaticValues.ClaimDetailInternalStatus>();
            foreach (var claimDetail in claimHeader.ClaimDetails)
            {
                claimHeaderInteralStatus.Add(claimDetail.ClaimDetailID, (StaticValues.ClaimDetailInternalStatus)claimDetail.ClaimDetailInternalStatus.GetValueOrDefault(0));
                claimDetail.ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.Finalized;
            }

            foreach (var claimDetail in claimHeader.ClaimDetails)
            {

                if (updateEdDataPlugin.UpdateEDData(claimDetail, claimDetail.PolicyCoverageID))
                {
                    Logger.InfoFormat("Executed \"UpdateExcessAndDeductibles\" successfully\r\n{0}\r\n",
                                      JObject.FromObject(new { claimHeader.ClaimReference, claimDetail.ClaimDetailReference }));
                }
                else
                {
                    Logger.InfoFormat("Executed \"UpdateExcessAndDeductibles\" but there was a problem\r\n{0}\r\n",
                                      JObject.FromObject(new { claimHeader.ClaimReference, claimDetail.ClaimDetailReference }));
                }
            }
            foreach (var kvp in claimHeaderInteralStatus)
            {
                var claimDetail = claimHeader.ClaimDetails.FirstOrDefault(cd => cd.ClaimDetailID == kvp.Key);
                if (claimDetail == null) continue;
                claimDetail.ClaimDetailInternalStatus = (short)kvp.Value;
            }

            return true;
        }


        

    }
}
