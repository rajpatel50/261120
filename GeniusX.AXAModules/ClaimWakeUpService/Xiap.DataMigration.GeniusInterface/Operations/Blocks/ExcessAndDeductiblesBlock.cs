using System.Collections.Generic;
using AutoMapper;
using Xiap.DataMigration.GeniusInterface.AXACS.ExcessAndDeductibles;
using Xiap.Metadata.Data.Enums;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Claims.BusinessComponent;
    using Entities;
    using Framework;
    using GeniusX.AXA.Claims.BusinessLogic;
    using Handlers;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json.Linq;
    using log4net;

    [BlockDescription("Execute Excess & Deductibles", Description = "")]
    public class ExcessAndDeductiblesBlock : IBlock
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExcessAndDeductiblesBlock));

        public static Claim Execute(Claim claim)
        {
            var sw = Stopwatch.StartNew();
            if (!ShouldExecute(claim)) return claim;
            var transaction = claim.CreateAmendClaimWithoutValidationTransaction();
            if (transaction == null) return claim;
            try
            {

                var claimHeader = transaction.ClaimHeader;
                claim.ExcessAndDeductiblesToProcess = true;
                bool policyExists;
                if (!GlobalClaimWakeUp.GeniusXPolicyState.TryGetValue(claim.PolicyNumber, out policyExists) || !policyExists) return claim;

                if (!claimHeader.PolicyDeductible01.HasValue &&
                    !claimHeader.PolicyDeductible02.HasValue &&
                    !claimHeader.PolicyDeductible03.HasValue &&
                    !claimHeader.PolicyDeductible04.HasValue &&
                    !claimHeader.PolicyDeductible05.HasValue)
                {
                    Logger.InfoFormat("No excess or deductibles to calculate for\r\n{0}\r\n", JObject.FromObject(new { claim.ClaimReference }));
                    claim.ExcessAndDeductiblesToProcess = false;
                    return claim;
                }
                // If we didn't have to create a policy then we can execute E&D
                if (ExcessAndDeductiblesProcessing(claim, claimHeader))
                {
                    if (claim.ClaimIsClosed) claim.ClaimProcessingCompleted = true;
                    transaction.Complete(false);
                }
                else
                    transaction.Cancel();

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
                var workDone = GlobalClaimWakeUp.Statistics.GetOrAdd(typeof(ExcessAndDeductiblesBlock).Name, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                GlobalClaimWakeUp.Statistics[typeof(ExcessAndDeductiblesBlock).Name] = (workDone + TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
            }
        }

        private static bool ShouldExecute(Claim claim)
        {
            var config = GlobalClaimWakeUp.Container.Resolve<ClaimProcessorConfiguration>();
            return config.TaskIsEnabled[typeof(ExcessAndDeductiblesBlock)] &&
             (!claim.ClaimIsDuplicate && !claim.ClaimProcessingFailed && !claim.ClaimAlreadyProcessed);
        }

        private static bool ExcessAndDeductiblesProcessing(Claim claim, ClaimHeader claimHeader)
        {
            try
            {

            using (var context = ClaimsEntitiesFactory.GetClaimsEntities())
            {
                // Pre: Policy information has been populated in clm
                // Post: Deductible position is calculated

                if (!claimHeader.HasHistoricalFinancialTransaction)
                {
                    Logger.WarnFormat("No E&D to execute for \r\n{0}\r\n", JObject.FromObject(new { claimHeader.ClaimReference, claimHeader.UWHeader.HeaderReference }));
                    return true;
                }
                var claimTransactionHeaderIds = claimHeader.HistoricalClaimTransactionHeaders.Select(t => t.ClaimTransactionHeaderID).ToArray();
                //if (!claimTransactionHeaderIds.Any()) return true;
                var calculateDeductiblesHandler = new CalculateDeductiblesHandler();
                //var axaCalculateDeductiblePlugin = new AXACalculateDeductiblePlugin();
                foreach (var inProgressClaimTransactionHeader in claimHeader.InProgressClaimTransactionHeaders)
                {
                    inProgressClaimTransactionHeader.SetToNotInProgress();
                }
                var transactionsByDay = context.ClaimTransactionHeader
                    .Where(t => claimTransactionHeaderIds.Contains(t.ClaimTransactionHeaderID))
                    .ToArray()
                    .GroupBy(t => t.TransactionDate.GetValueOrDefault(DateTime.MinValue).Date)
                    .OrderBy(g => g.Key)
                    .ToArray();
                
                foreach (var day in transactionsByDay)
                {
                    var reserveStack = new Stack<ClaimTransactionHeader>(from t in day
                                                                         where 
                                                                            t.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Reserve || 
                                                                            t.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReserve
                                                                         orderby t.TransactionDate descending 
                                                                         select t);

                    var otherTransactionsStack = new Stack<ClaimTransactionHeader>(from t in day
                                                                                   where
                                                                                      t.ClaimTransactionSource != (short)StaticValues.ClaimTransactionSource.Reserve &&
                                                                                      t.ClaimTransactionSource != (short)StaticValues.ClaimTransactionSource.RecoveryReserve
                                                                                   orderby t.ClaimTransactionHeaderID descending
                                                                                   select t);
                    if (otherTransactionsStack.Count < reserveStack.Count)
                    {
                        while (otherTransactionsStack.Count != 0)
                        {
                            Logger.Info("Found non-reserve transaction, checking for reserve");
                            var t0 = otherTransactionsStack.Pop();
                            if (reserveStack.Count != 0)
                            {
                                var t1 = reserveStack.Pop();
                                t0.TransactionDate =
                                    t1.TransactionDate.GetValueOrDefault(DateTime.MinValue).AddSeconds(5);
                                Logger.InfoFormat("Reserve found:\r\n{0}", JObject.FromObject(
                                    new
                                        {
                                            claim.ClaimReference,
                                            OtherTransaction = t0.ClaimTransactionHeaderReference,
                                            OtherTransactionDate = t0.TransactionDate,
                                            ReserveTransaction = t1.ClaimTransactionHeaderReference,
                                            ReserveTransactionDate = t1.TransactionDate
                                        }));
                            }

                        }
                    }

                }

                var orderedTransactions = transactionsByDay.SelectMany(g => g).OrderBy(t => t.TransactionDate);
                Logger.InfoFormat("All transaction headers for Claim\r\n{0}\r\n", JObject.FromObject(new { claim.ClaimReference, Headers = orderedTransactions.Select(t => t.ClaimTransactionHeaderReference).ToArray() }));

                var claimTransactionDetails = (from cth in orderedTransactions.ToArray()
                                             from ctg in cth.ClaimTransactionGroups.ToArray()
                                             from ctd in ctg.ClaimTransactionDetails.ToArray()
                                             select ctd)
                                            .ToArray();
                List<FlattenedTransaction> flattenedTransactions = claimTransactionDetails
                    .Select(Mapper.Map<ClaimTransactionDetail, FlattenedTransaction>)
                    .ToList();
                GlobalClaimWakeUp.MappedTransactionDetails.TryAdd(claimHeader.ClaimReference, flattenedTransactions);
                
                foreach (var claimTransactionHeader in orderedTransactions)
                {
                    ProcessTransaction(claimHeader, claimTransactionHeader, calculateDeductiblesHandler);
                    Logger.InfoFormat("Processed\r\n{0}",
                                      JObject.FromObject(
                                          new
                                              {
                                                  claim.ClaimReference,
                                                  ReserveTransaction = claimTransactionHeader.ClaimTransactionHeaderReference
                                              }));
                }
                
                GlobalClaimWakeUp.ClearAttachedData(claimHeader.ClaimReference);
                context.SaveChanges();
                return true;
            }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Unhandled exception:\r\n{0}", JObject.FromObject(ex));
                return false;
            }
        }

        private static void ProcessTransaction(
            ClaimHeader claimHeader,
            ClaimTransactionHeader claimTransactionHeader, 
            CalculateDeductiblesHandler calculateDeductiblesHandler)
        {
            try
            {
                claimTransactionHeader.Context = claimHeader.Context;
                claimTransactionHeader.SetToInProgress();
                //axaCalculateDeductiblePlugin.ProcessComponent(claimTransactionHeader, ProcessInvocationPoint.Virtual, 0);
                calculateDeductiblesHandler.Calculate(claimTransactionHeader);
                claimTransactionHeader.SetToNotInProgress();

                GlobalClaimWakeUp.ClearAttachedData(claimHeader.ClaimReference);
                Logger.InfoFormat("Executed Deductible calculation for\r\n{0}\r\n", JObject.FromObject(new { claimHeader.ClaimReference, claimTransactionHeader.ClaimTransactionHeaderReference }));
            }
            catch (Exception ex)
            {

                Logger.ErrorFormat("Exception thrown while executing E&D\r\n{0}\r\n",
                    JObject.FromObject(new { claimHeader.ClaimReference, claimTransactionHeader.ClaimTransactionHeaderReference, ex.Message, ex.StackTrace }));
            }

        }
    }
}
