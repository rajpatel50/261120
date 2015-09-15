namespace Xiap.DataMigration.GeniusInterface.AXACS.ExcessAndDeductibles
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Collections.Generic;
    using AutoMapper;
    using Claims.BusinessComponent;
    using Entities;

    public static class ExtensionMethods
    {
        public static IEnumerable<FlattenedTransaction> GetFlattenedTransactionData(this ClaimHeader source)
        {
            List<FlattenedTransaction> flattenedTransactions;
            if (!GlobalClaimWakeUp.MappedTransactionDetails.TryGetValue(source.ClaimReference, out flattenedTransactions))
            {
                var claimTransactions = (from cth in source.HistoricalClaimTransactionHeaders.ToArray().Concat(source.InProgressClaimTransactionHeaders.ToArray())
                                        from ctg in cth.ClaimTransactionGroups.ToArray()
                                        from ctd in ctg.ClaimTransactionDetails.ToArray()
                                        select ctd)
                                        .ToArray();

                flattenedTransactions = claimTransactions
                    .Select(Mapper.Map<ClaimTransactionDetail, FlattenedTransaction>)
                    .ToList();
                GlobalClaimWakeUp.MappedTransactionDetails.TryAdd(source.ClaimReference, flattenedTransactions);
            }
            return flattenedTransactions;
        }

        public static long SafeParseToInt(this string source, long? @default = long.MinValue)
        {
            long num;
            if (!long.TryParse(source, out num)) return @default.GetValueOrDefault(long.MinValue);
            return num;
        }

        public static void SetToInProgress(this ClaimTransactionHeader source)
        {
            source.IsInProgress = true;
            foreach (var grp in source.ClaimTransactionGroups)
            {
                grp.IsInProgress = true;
            }
        }

        public static void SetToNotInProgress(this ClaimTransactionHeader source)
        {
            source.IsInProgress = false;
            foreach (var grp in source.ClaimTransactionGroups)
            {
                grp.IsInProgress = false;
            }
        }
        //public static IEnumerable<FlattenedTransaction> GetViableTransactions(this ClaimHeader source)
        //{
        //    CurrentTransactionInformationWindow cacheItem;

        //    var transactions = source
        //            .GetFlattenedTransactionData()
        //            .OrderBy(t => t.TransactionDate)
        //            .ThenBy(t => t.ClaimTransactionHeaderID)
        //            .AsEnumerable();
        //    var inProgressTransaction = source.InProgressClaimTransactionHeaders.FirstOrDefault();
        //    if (!_cache.TryGetValue(source.ClaimReference, out cacheItem))
        //    {
        //        cacheItem = new CurrentTransactionInformationWindow();
                
        //        if (inProgressTransaction != null)
        //        {
        //            transactions = transactions
        //                .TakeWhile(t => t.ClaimTransactionHeaderID != inProgressTransaction.ClaimTransactionHeaderID)
        //                .ToArray();
        //            cacheItem.LastInProgressClaimTransactionHeaderId = inProgressTransaction.ClaimTransactionHeaderID;
        //        }
        //        cacheItem.LastAccumulatedTransactions = transactions;
        //        _cache.TryAdd(source.ClaimReference, cacheItem);
        //    }
        //    else
        //    {
        //        if (inProgressTransaction != null)
        //        {
        //            transactions = transactions
        //                .SkipWhile(t => t.ClaimTransactionHeaderID != cacheItem.LastInProgressClaimTransactionHeaderId)
        //                .TakeWhile(t => t.ClaimTransactionHeaderID != inProgressTransaction.ClaimTransactionHeaderID)
        //                .ToArray();
        //            cacheItem.LastInProgressClaimTransactionHeaderId = inProgressTransaction.ClaimTransactionHeaderID;
        //        }
        //        else
        //        {
        //            _cache.TryRemove(source.ClaimReference, out cacheItem);
        //        }

        //        cacheItem.LastAccumulatedTransactions = cacheItem.LastAccumulatedTransactions.Concat(transactions);
        //    }
        //    return cacheItem.LastAccumulatedTransactions;
        //}

        //private static ConcurrentDictionary<string, CurrentTransactionInformationWindow> _cache = new ConcurrentDictionary<string,CurrentTransactionInformationWindow>();

        //public class CurrentTransactionInformationWindow
        //{
        //    public long LastInProgressClaimTransactionHeaderId { get; set; }

        //    public IEnumerable<FlattenedTransaction> LastAccumulatedTransactions { get; set; }
        //}
    }
}
