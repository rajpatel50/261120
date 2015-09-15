namespace Xiap.DataMigration.GeniusInterface.AXACS.ExcessAndDeductibles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Claims.BusinessComponent;
    using Claims.BusinessLogic.AuthorityCheck;
    using Entities;
    using Framework;
    using Framework.Security;
    using Framework.Validation;
    using Metadata.Data.Enums;
    using log4net;
    using ClaimDetail = Claims.BusinessComponent.ClaimDetail;
    using ClaimFinancialAmount = Xiap.Framework.Data.Claims.ClaimFinancialAmount;
    using ClaimStaticValues = Xiap.Framework.Data.Claims;

    public class MigrationHistoricalFinancialAmountData : IHistoricalFinancialAmountData
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (MigrationHistoricalFinancialAmountData));
        private const int ClaimLevel = 1;
        private const int ClaimDetailLevel = 2;
        private const int ClaimTransactionLevel = 3;

        public IEnumerable<ClaimFinancialAmount> ReadClaimAmountData(ClaimHeader claimHeader, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimHeader, "claimHeader");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug(string.Format("ReadClaimAmountData({0}, {1})", claimHeader.ClaimHeaderID, amountType));
            }

            return this.ExecuteAccumulatedClaimAmounts(claimHeader, ClaimLevel, claimHeader.ClaimHeaderID, amountType, claimHeader.Context, false);
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimDetail, "claimDetail");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug(string.Format("ReadClaimDetailAmountData({0}, {1})", claimDetail.ClaimDetailID, amountType));
            }

            return this.ExecuteAccumulatedClaimAmounts(claimDetail.ClaimHeader, ClaimDetailLevel, claimDetail.ClaimDetailID, amountType, claimDetail.Context, false);
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimTransactionHeader, "claimTransactionHeader");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug(string.Format("ReadTransactionAmountData({0}, {1})", claimTransactionHeader.ClaimTransactionHeaderID, amountType));
            }

            return this.ReadTransactionAmountData(claimTransactionHeader, amountType, false);
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, bool includeRejectedAmount)
        {
            ArgumentCheck.ArgumentNullCheck(claimTransactionHeader, "claimTransactionHeader");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug(string.Format("ReadTransactionAmountData({0}, {1}, {2})", claimTransactionHeader.ClaimTransactionHeaderID, amountType, includeRejectedAmount));
            }

            return this.ExecuteAccumulatedClaimAmounts(claimTransactionHeader.ClaimHeader, ClaimTransactionLevel, claimTransactionHeader.ClaimTransactionHeaderID, amountType, claimTransactionHeader.Context, includeRejectedAmount);
        }

        public IEnumerable<ClaimFinancialAmount> ReadLatestClaimAmountData(ClaimHeader claimHeader, bool includeEstimated, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimHeader, "claimHeader");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug(string.Format("ReadLatestClaimAmountData({0}, {1}, {2})", claimHeader.ClaimHeaderID, includeEstimated, amountType));
            }

            return this.ExecuteLatestClaimAmounts(claimHeader, ClaimLevel, claimHeader.ClaimHeaderID, amountType, includeEstimated, claimHeader.Context);
        }

        public IEnumerable<ClaimFinancialAmount> ReadLatestClaimDetailAmountData(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimDetail, "claimDetail");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug(string.Format("ReadLatestClaimDetailAmountData({0}, {1}, {2})", claimDetail.ClaimDetailID, includeEstimated, amountType));
            }

            return this.ExecuteLatestClaimAmounts(claimDetail.ClaimHeader, ClaimDetailLevel, claimDetail.ClaimDetailID, amountType, includeEstimated, claimDetail.Context);
        }

        public IEnumerable<ClaimFinancialAmount> ReadUsersClaimPaymentAmountData(ClaimHeader claimHeader, SecurityUser user, StaticValues.AuthorisationResult authorisationResult)
        {
            ArgumentCheck.ArgumentNullCheck(claimHeader, "claimHeader");
            ArgumentCheck.ArgumentNullCheck(user, "user");
            ArgumentCheck.ArgumentNullCheck(authorisationResult, "authorisationResult");

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug(string.Format("ReadUsersClaimPaymentAmountData({0}, {1}, {2})", claimHeader.ClaimHeaderID, user.UserIdentity, authorisationResult));
            }


            var list = GlobalClaimWakeUp.GetAttachedData<ClaimPaymentAmountData>(claimHeader.ClaimReference);
            var item =
                list.FirstOrDefault(
                    data =>
                    data.ClaimHeaderID == claimHeader.ClaimHeaderID && data.UserID == user.UserID &&
                    data.AuthorisationResult == (short) authorisationResult);
            if (item != null)
                return item.ClaimPaymentAmounts;

            var transactions = claimHeader
                .GetFlattenedTransactionData()
                .OrderBy(t => t.TransactionDate)
                .AsEnumerable();
            
            var inProgressTransaction = claimHeader.InProgressClaimTransactionHeaders.FirstOrDefault();
            if (inProgressTransaction != null)
                transactions = transactions
                    .TakeWhile(t => t.ClaimTransactionHeaderReference.SafeParseToInt() != inProgressTransaction.ClaimTransactionHeaderReference.SafeParseToInt())
                    .ToArray();
            
            var query = from t in transactions
                        where
                            t.ClaimTransactionSource == (short) StaticValues.ClaimTransactionSource.Payment &&
                            t.PaymentAuthorisationStatus == (short) StaticValues.PaymentAuthorisationStatus.PaymentAuthorised &&
                            t.AmountType == (short) StaticValues.AmountType.Payment &&
                            t.AuthorisationLogs.Any(
                                al => al.AmountType == (short)StaticValues.AmountType.Payment &&
                                      al.AuthorisationResult == (short)authorisationResult &&
                                      al.ActionedByUserID == user.UserID
                                      )
                        select new ClaimFinancialAmount
                                   {
                                       ClaimDetailReference = t.ClaimDetailReference,
                                       MovementType = t.MovementType,
                                       AccountingCurrencyCode = t.AccountingCurrencyCode,
                                       OriginalCurrencyCode = t.OriginalCurrencyCode,
                                       ClaimCurrencyCode = t.ClaimCurrencyCode,
                                       TransactionAmountAccounting = t.TransactionAmountAccounting,
                                       TransactionAmountBase = t.TransactionAmountBase,
                                       TransactionAmountClaimCurrency = t.TransactionAmountClaimCurrency,
                                       TransactionAmountOriginal = t.TransactionAmountOriginal,
                                       MovementAmountAccounting = t.MovementAmountAccounting,
                                       MovementAmountBase = t.MovementAmountBase,
                                       MovementAmountClaimCurrency = t.MovementAmountClaimCurrency,
                                       MovementAmountOriginal = t.MovementAmountOriginal,
                                   };

            var claimFinancialAmounts = query.ToList();
            GlobalClaimWakeUp.AddAttachedData<ClaimPaymentAmountData>(claimHeader.ClaimReference, new ClaimPaymentAmountData(claimHeader.ClaimHeaderID, user.UserID, (short)authorisationResult, claimFinancialAmounts));

            return claimFinancialAmounts;
        }

        private List<ClaimFinancialAmount> ExecuteAccumulatedClaimAmounts(ClaimHeader claimHeader, int level, long id, StaticValues.AmountType amountType, TransactionContext transactionContext, bool includeRejectedAmount)
        {
            var list = GlobalClaimWakeUp.GetAttachedData<AccumulatedClaimAmountsData>(claimHeader.ClaimReference);
            var item = list.FirstOrDefault(data => data.Level == level && data.Id == id && data.AmountType == amountType);
            if (item != null)
                return item.AccumulatedClaimFinancialAmounts;

            var transactions = claimHeader
                .GetFlattenedTransactionData()
                .OrderBy(t => t.TransactionDate)
               .AsEnumerable();

            var inProgressTransaction = claimHeader.InProgressClaimTransactionHeaders.FirstOrDefault();
            if (inProgressTransaction != null)
                transactions = transactions
                    .TakeWhile(t => t.ClaimTransactionHeaderReference.SafeParseToInt() != inProgressTransaction.ClaimTransactionHeaderReference.SafeParseToInt())
                    .ToArray();
            
            transactions = transactions.Where(x => x.AmountType == (short)amountType);
            switch (level)
            {
                case 1:
                    transactions = transactions.Where(x => x.ClaimHeaderID == id);
                    break;

                case 2:
                    transactions = transactions.Where(x => x.ClaimDetailID == id);
                    break;

                case 3:
                    transactions = transactions.Where(x => x.ClaimTransactionHeaderID == id);
                    break;
            }
            
            if (!includeRejectedAmount)
            {
                switch (amountType)
                {
                    case StaticValues.AmountType.Reserve:
                        transactions = transactions.Where(x => x.ReserveAuthorisationStatus.GetValueOrDefault(0) != (short)StaticValues.ReserveAuthorisationStatus.ReserveRejected);
                        break;
                    case StaticValues.AmountType.Payment:
                        transactions = transactions.Where(x => x.PaymentAuthorisationStatus.GetValueOrDefault(0) != (short)StaticValues.PaymentAuthorisationStatus.PaymentRejected);
                        break;
                    case StaticValues.AmountType.RecoveryReserve:
                        transactions = transactions.Where(x =>x.RecoveryReserveAuthorisationStatus.GetValueOrDefault(0) != (short)StaticValues.RecoveryReserveAuthorisationStatus.RecoveryReserveRejected);
                        break;
                    case StaticValues.AmountType.RecoveryReceipt:
                        transactions = transactions.Where(x => x.RecoveryReceiptAuthorisationStatus.GetValueOrDefault(0) != (short)StaticValues.RecoveryReceiptAuthorisationStatus.RecoveryReceiptRejected);
                        break;
                }
            }
            
            var result = transactions.GroupBy(t => 
                    new
                    {
                        t.ClaimDetailReference,
                        t.MovementType,
                        t.AccountingCurrencyCode,
                        t.OriginalCurrencyCode,
                        t.ClaimCurrencyCode,
                    })
                    //.AsParallel()
                    .Select(grp => new ClaimFinancialAmount
                            {
                                AmountType = (ClaimStaticValues.AmountType)amountType,
                                ClaimDetailReference = grp.Key.ClaimDetailReference,
                                MovementType = grp.Key.MovementType,
                                AccountingCurrencyCode = grp.Key.AccountingCurrencyCode,
                                OriginalCurrencyCode = grp.Key.OriginalCurrencyCode,
                                ClaimCurrencyCode = grp.Key.ClaimCurrencyCode,
                                TransactionAmountAccounting = grp.Sum(x => x.TransactionAmountAccounting),
                                TransactionAmountBase = grp.Sum(x => x.TransactionAmountBase),
                                TransactionAmountClaimCurrency = grp.Sum(x => x.TransactionAmountClaimCurrency),
                                TransactionAmountOriginal = grp.Sum(x => x.TransactionAmountOriginal),
                                MovementAmountAccounting = grp.Sum(x => x.MovementAmountAccounting),
                                MovementAmountBase = grp.Sum(x => x.MovementAmountBase),
                                MovementAmountClaimCurrency = grp.Sum(x => x.MovementAmountClaimCurrency),
                                MovementAmountOriginal = grp.Sum(x => x.MovementAmountOriginal)
                            }).ToList();
                        
            GlobalClaimWakeUp.AddAttachedData<AccumulatedClaimAmountsData>(claimHeader.ClaimReference, new AccumulatedClaimAmountsData(level, id, amountType, result));

            return result;
        }


        private List<ClaimFinancialAmount> ExecuteLatestClaimAmounts(ClaimHeader claimHeader, int level, long id, StaticValues.AmountType amountType, bool includeEstimated, TransactionContext transactionContext)
        {
            var list = GlobalClaimWakeUp.GetAttachedData<LatestClaimAmountsData>(claimHeader.ClaimReference);

            var item =
                list.FirstOrDefault(
                    data =>
                    data.Level == level && data.Id == id && data.AmountType == amountType && data.IncludeEstimated == includeEstimated);
            if (item != null)
                return item.LatestClaimFinancialAmounts;
            
            var transactions = claimHeader
                .GetFlattenedTransactionData()
                .OrderBy(t => t.TransactionDate)
                .AsEnumerable();

            var inProgressTransaction = claimHeader.InProgressClaimTransactionHeaders.FirstOrDefault();
            if (inProgressTransaction != null)
                transactions = transactions.TakeWhile(t => t.ClaimTransactionHeaderReference.SafeParseToInt() != inProgressTransaction.ClaimTransactionHeaderReference.SafeParseToInt()).ToArray();

            //var transactions = claimHeader.GetViableTransactions();
            List<ClaimFinancialAmount> claimFinancialAmounts;
            if (level == 1)
                claimFinancialAmounts = LevelOneLatest(id, amountType, transactions, inProgressTransaction, includeEstimated).ToList();
            else
                claimFinancialAmounts = LevelTwoLatest(id, amountType, transactions, inProgressTransaction, includeEstimated).ToList();

            GlobalClaimWakeUp.AddAttachedData<LatestClaimAmountsData>(claimHeader.ClaimReference, new LatestClaimAmountsData(level, id, amountType, includeEstimated, claimFinancialAmounts));

            return claimFinancialAmounts;
        }

        private static IEnumerable<ClaimFinancialAmount> LevelTwoLatest(long id, StaticValues.AmountType amountType, IEnumerable<FlattenedTransaction> transactions, ClaimTransactionHeader inProgressTransaction, bool includeEstimate)
        {
            var query = from t in transactions
                        //from ctg in cth.TransactionGroups
                        //from ctd in ctg.TransactionDetails
                        where t.AmountType == (short)amountType
                        select t;

            if (!includeEstimate)
            {
                query = query.Where(x => x.ReserveType != (short) StaticValues.ReserveType.Estimated);
            }
            query = query.Where(x => x.ClaimDetailID == id &&
                            (amountType == StaticValues.AmountType.Reserve || x.RecoveryReserveAuthorisationStatus.GetValueOrDefault(0) != (short)StaticValues.RecoveryReceiptAuthorisationStatus.RecoveryReceiptRejected) &&
                            (amountType == StaticValues.AmountType.RecoveryReserve || x.ReserveAuthorisationStatus.GetValueOrDefault(0) != (short)StaticValues.ReserveAuthorisationStatus.ReserveRejected)
                            );
            
            //IEnumerable<ClaimFinancialAmount>
            if (includeEstimate)
                return GetTopRankedTransactionIncludingEstimated(query, amountType);

            return GetTopRankedTransactionWithoutEstimated(query, amountType);
        }

        

        private static IEnumerable<ClaimFinancialAmount> LevelOneLatest(long id, StaticValues.AmountType amountType, IEnumerable<FlattenedTransaction> transactions, ClaimTransactionHeader inProgressTransaction, bool includeEstimate)
        {
            var query =  from t in transactions
                   where t.AmountType == (short)amountType
                   select t;
            
            if (!includeEstimate)
            {
                query = query.Where(x => x.ReserveType != (short) StaticValues.ReserveType.Estimated);
            }
           query = query.Where(x => x.ClaimHeaderID == id &&
                            (amountType == StaticValues.AmountType.Reserve || x.RecoveryReserveAuthorisationStatus.GetValueOrDefault(0) != (short)StaticValues.RecoveryReceiptAuthorisationStatus.RecoveryReceiptRejected) &&
                            (amountType == StaticValues.AmountType.RecoveryReserve || x.ReserveAuthorisationStatus.GetValueOrDefault(0) != (short)StaticValues.ReserveAuthorisationStatus.ReserveRejected)
                            );

           if (includeEstimate)
               return GetTopRankedTransactionIncludingEstimated(query, amountType);

           return GetTopRankedTransactionWithoutEstimated(query, amountType);
        }

        private static IEnumerable<ClaimFinancialAmount> GetTopRankedTransactionIncludingEstimated(IEnumerable<FlattenedTransaction> transactions, StaticValues.AmountType amountType)
        {
            var result = transactions.GroupBy(
                t =>
                new
                {
                    t.ClaimDetailID,
                    t.MovementType,
                    t.AccountingCurrencyCode,
                    t.OriginalCurrencyCode,
                    t.ReserveType
                })
                //.AsParallel()
                .Select(grp =>
                    // Rank the transactions.  
                        grp
                            .OrderByDescending(ctd => ctd.ReserveDate)
                            .ThenByDescending(ctd => ctd.ReserveDaySequence.GetValueOrDefault(short.MinValue))
                            .Select((ctd, i) =>
                              new
                              {
                                  Rank = i + 1,
                                  ClaimFinancialAmount = new ClaimFinancialAmount
                                  {
                                      AmountType = (ClaimStaticValues.AmountType)amountType,
                                      ClaimDetailReference = ctd.ClaimDetailReference,
                                      TransactionAmountAccounting = ctd.TransactionAmountAccounting,
                                      TransactionAmountClaimCurrency = ctd.TransactionAmountClaimCurrency,
                                      TransactionAmountBase = ctd.TransactionAmountBase,
                                      TransactionAmountOriginal = ctd.TransactionAmountOriginal,
                                      MovementAmountAccounting = ctd.MovementAmountAccounting,
                                      MovementAmountClaimCurrency = ctd.MovementAmountClaimCurrency,
                                      MovementAmountBase = ctd.MovementAmountBase,
                                      MovementAmountOriginal = ctd.MovementAmountOriginal,
                                      MovementType = ctd.MovementType,
                                      ReserveType = (ClaimStaticValues.ReserveType)Convert.ToInt16(ctd.ReserveType),
                                      OriginalCurrencyCode = grp.Key.OriginalCurrencyCode,
                                      AccountingCurrencyCode = grp.Key.AccountingCurrencyCode,
                                      ClaimCurrencyCode = grp.First().ClaimCurrencyCode,
                                      ReserveDate = ctd.ReserveDate,
                                      ReserveDaySequence = ctd.ReserveDaySequence,
                                      OrderShareCoinsurance = (ClaimStaticValues.OrderShareCoinsurance)ctd.OrderShareCoinsurance,
                                      CalculationSourceAmountOriginal = ctd.CalculationSourceAmountOriginal,
                                      ClaimTransactionDetailID = ctd.ClaimTransactionDetailID
                                  }
                              })
                       );


            return result.Select(x => x.First(y => y.Rank == 1).ClaimFinancialAmount);
        }

        private static IEnumerable<ClaimFinancialAmount> GetTopRankedTransactionWithoutEstimated(IEnumerable<FlattenedTransaction> transactions, StaticValues.AmountType amountType)
        {
            var result = transactions.GroupBy(
                t =>
                new
                {
                    t.ClaimDetailID,
                    t.MovementType,
                    t.AccountingCurrencyCode,
                    t.OriginalCurrencyCode,
                })
                //.AsParallel()
                .Select(grp =>
                    // Rank the transactions.  
                        grp
                            .OrderByDescending(ctd => ctd.ReserveDate)
                            .ThenByDescending(ctd => ctd.ReserveDaySequence.GetValueOrDefault(short.MinValue))
                            .Select((ctd, i) =>
                              new
                              {
                                  Rank = i + 1,
                                  ClaimFinancialAmount = new ClaimFinancialAmount
                                  {
                                      AmountType = (ClaimStaticValues.AmountType)amountType,
                                      ClaimDetailReference = ctd.ClaimDetailReference,
                                      TransactionAmountAccounting = ctd.TransactionAmountAccounting,
                                      TransactionAmountClaimCurrency = ctd.TransactionAmountClaimCurrency,
                                      TransactionAmountBase = ctd.TransactionAmountBase,
                                      TransactionAmountOriginal = ctd.TransactionAmountOriginal,
                                      MovementAmountAccounting = ctd.MovementAmountAccounting,
                                      MovementAmountClaimCurrency = ctd.MovementAmountClaimCurrency,
                                      MovementAmountBase = ctd.MovementAmountBase,
                                      MovementAmountOriginal = ctd.MovementAmountOriginal,
                                      MovementType = ctd.MovementType,
                                      ReserveType = (ClaimStaticValues.ReserveType)ctd.ReserveType,
                                      OriginalCurrencyCode = grp.Key.OriginalCurrencyCode,
                                      AccountingCurrencyCode = grp.Key.AccountingCurrencyCode,
                                      ClaimCurrencyCode = grp.First().ClaimCurrencyCode,
                                      ReserveDate = ctd.ReserveDate,
                                      ReserveDaySequence = ctd.ReserveDaySequence,
                                      OrderShareCoinsurance = (ClaimStaticValues.OrderShareCoinsurance)ctd.OrderShareCoinsurance,
                                      CalculationSourceAmountOriginal = ctd.CalculationSourceAmountOriginal,
                                      ClaimTransactionDetailID = ctd.ClaimTransactionDetailID
                                  }
                              })
                       );
                       

            return result.Select(x => x.First(y => y.Rank == 1).ClaimFinancialAmount); ;
        }

        private T? DbNullableConvert<T>(Func<object, T> converter, object value) where T : struct
        {
            if (value == System.DBNull.Value)
            {
                return null;
            }

            return converter(value);
        }

        private class LatestClaimAmountsData
        {
            public readonly int Level;
            public readonly long Id;
            public readonly StaticValues.AmountType AmountType;
            public readonly bool IncludeEstimated;
            public readonly List<ClaimFinancialAmount> LatestClaimFinancialAmounts;

            public LatestClaimAmountsData(int level, long id, StaticValues.AmountType amountType, bool includeEstimated, List<ClaimFinancialAmount> latestClaimFinancialAmounts)
            {
                this.Level = level;
                this.Id = id;
                this.AmountType = amountType;
                this.IncludeEstimated = includeEstimated;
                this.LatestClaimFinancialAmounts = latestClaimFinancialAmounts;
            }
        }

        private class AccumulatedClaimAmountsData
        {
            public readonly int Level;
            public readonly long Id;
            public readonly StaticValues.AmountType AmountType;
            public readonly List<ClaimFinancialAmount> AccumulatedClaimFinancialAmounts;

            public AccumulatedClaimAmountsData(int level, long id, StaticValues.AmountType amountType, List<ClaimFinancialAmount> accumulatedClaimFinancialAmounts)
            {
                this.Level = level;
                this.Id = id;
                this.AmountType = amountType;
                this.AccumulatedClaimFinancialAmounts = accumulatedClaimFinancialAmounts;
            }
        }

        private class ClaimPaymentAmountData
        {
            public readonly long ClaimHeaderID;
            public readonly long UserID;
            public readonly short AuthorisationResult;
            public readonly List<ClaimFinancialAmount> ClaimPaymentAmounts;

            public ClaimPaymentAmountData(long claimHeaderID, long userID, short authorisationResult, List<ClaimFinancialAmount> claimPaymentAmounts)
            {
                this.ClaimHeaderID = claimHeaderID;
                this.UserID = userID;
                this.AuthorisationResult = authorisationResult;
                this.ClaimPaymentAmounts = claimPaymentAmounts;
            }
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimAmountData(ClaimHeader claimHeader, StaticValues.AmountType amountType, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, StaticValues.AmountType amountType, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadLatestClaimAmountData(ClaimHeader claim, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadLatestClaimDetailAmountData(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadLatestClaimDetailAmountDataWithReinsurance(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, bool includeRejectedAmount, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadUsersClaimPaymentAmountData(ClaimHeader claimHeader, SecurityUser user, StaticValues.AuthorisationResult authorisationResult, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountDataWithReinsurance(ClaimDetail claimDetail, StaticValues.AmountType amountType, bool? isNonFinancial)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimAmountData(ClaimHeader claimHeader, StaticValues.AmountType amountType, bool? isNonFinancial, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, StaticValues.AmountType amountType, bool? isNonFinancial, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadLatestClaimAmountData(ClaimHeader claim, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isNonFinancial, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadLatestClaimDetailAmountData(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isNonFinancial, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadLatestClaimDetailAmountDataWithReinsurance(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isNonFinancial, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool includeRejectedAmount, bool? isNonFinancial, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, bool includeRejectedAmount, bool? isNonFinancial, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, bool includeRejectedAmount, bool? isNonFinancial, FinancialAmountParticipationFilter filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, bool? isNonFinancial, FinancialAmountParticipationFilter filter)
        {
            throw new NotImplementedException();
        }
    }
}
