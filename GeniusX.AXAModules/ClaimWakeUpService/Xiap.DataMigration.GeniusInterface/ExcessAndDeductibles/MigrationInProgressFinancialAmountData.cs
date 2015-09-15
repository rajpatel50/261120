using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic.AuthorityCheck;
using Xiap.DataMigration.GeniusInterface.AXACS.Entities;
using Xiap.Framework.Logging;
using Xiap.Framework.Security;
using Xiap.Framework.Validation;
using Xiap.Metadata.Data.Enums;
using ClaimDetail = Xiap.Claims.BusinessComponent.ClaimDetail;
using ClaimFinancialAmount = Xiap.Framework.Data.Claims.ClaimFinancialAmount;

namespace Xiap.DataMigration.GeniusInterface.AXACS.ExcessAndDeductibles
{
    public class MigrationInProgressFinancialAmountData : IInProgressFinancialAmountData
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimTransactionHeader, "claimTransactionHeader");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ReadTransactionAmountData({0}, {1})", claimTransactionHeader.ClaimTransactionHeaderID, amountType));
            }
            var flattenedTransactions = claimTransactionHeader.ClaimHeader
                .GetFlattenedTransactionData()
                .Where(t => t.ClaimTransactionHeaderID == claimTransactionHeader.ClaimTransactionHeaderID);

            var transactions = from t in flattenedTransactions
                               where t.AmountType == (short)amountType && t.TransactionAmountOriginal != null
                               select CreateClaimFinancialAmount(t);

            return transactions.ToList();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, bool includeEstimated, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimTransactionHeader, "claimTransactionHeader");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ReadTransactionAmountData({0}, {1}, {2})", claimTransactionHeader.ClaimTransactionHeaderID, includeEstimated, amountType));
            }

            var flattenedTransactions = claimTransactionHeader.ClaimHeader
                .GetFlattenedTransactionData()
                .Where(t => t.ClaimTransactionHeaderID == claimTransactionHeader.ClaimTransactionHeaderID);

            var amounts = flattenedTransactions.Where(t => t.AmountType == (short)amountType);
            if (!includeEstimated) amounts = amounts.Where(t => t.ReserveType != (short)StaticValues.ReserveType.Estimated);
            amounts = amounts.Where(t => t.TransactionAmountOriginal != null);
                          
            return amounts.Select(CreateClaimFinancialAmount);
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimDetail, "claimDetail");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ReadClaimDetailAmountData({0}, {1})", claimDetail.ClaimDetailID, amountType));
            }

            var flattenedTransactions = claimDetail.ClaimHeader
                .GetFlattenedTransactionData()
                .Where(t => t.TransactionGroupIsInProgress && t.ClaimDetailReference == claimDetail.ClaimDetailReference);

            var amounts = from t in flattenedTransactions
                          where t.AmountType == (short)amountType
                          && t.TransactionAmountOriginal != null
                          select CreateClaimFinancialAmount(t);

            return amounts.ToList();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType)
        {
            ArgumentCheck.ArgumentNullCheck(claimDetail, "claimDetail");
            ArgumentCheck.ArgumentNullCheck(amountType, "amountType");

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ReadClaimDetailAmountData({0}, {1}, {2})", claimDetail.ClaimDetailID, includeEstimated, amountType));
            }

            var flattenedTransactions = claimDetail.ClaimHeader
                .GetFlattenedTransactionData()
                .Where(t => t.TransactionGroupIsInProgress && t.ClaimDetailReference == claimDetail.ClaimDetailReference);

            var amounts = flattenedTransactions.Where(t => t.AmountType == (short)amountType);
            if (!includeEstimated) amounts = amounts.Where(t => t.ReserveType != (short)StaticValues.ReserveType.Estimated);
            amounts = amounts.Where(t => t.TransactionAmountOriginal != null);

            return amounts.Select(t => CreateClaimFinancialAmount(t));
        }

        public IEnumerable<ClaimFinancialAmount> ReadUsersClaimPaymentAmountData(ClaimHeader claimHeader, SecurityUser user, StaticValues.AuthorisationResult authorisationResult)
        {
            ArgumentCheck.ArgumentNullCheck(claimHeader, "claimHeader");
            ArgumentCheck.ArgumentNullCheck(user, "user");
            ArgumentCheck.ArgumentNullCheck(authorisationResult, "authorisationResult");

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ReadUsersClaimPaymentAmountData({0}, {1}, {2})", claimHeader.ClaimHeaderID, user, authorisationResult));
            }

            var flattenedTransactions = claimHeader
                .GetFlattenedTransactionData()
                .Where(t => t.TransactionHeaderIsInProgress);

            var amounts = from t in flattenedTransactions
                          where t.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment
                          && !t.IsClaimPaymentCancelled.GetValueOrDefault(false)
                          && t.PaymentAuthorisationStatus == (short)StaticValues.PaymentAuthorisationStatus.PaymentAuthorised
                          && t.AuthorisationLogs.Any(a => a.ActionedByUserID == user.UserID && a.AuthorisationResult == (short)authorisationResult)
                          && t.TransactionAmountOriginal != null
                          select CreateClaimFinancialAmount(t);

            return amounts.ToList();
        }

        private static ClaimFinancialAmount CreateClaimFinancialAmount(FlattenedTransaction transaction)
        {
            return new ClaimFinancialAmount
            {
                ClaimDetailReference = transaction.ClaimDetailReference,
                MovementType = transaction.MovementType,
                AccountingCurrencyCode = transaction.AccountingCurrencyCode,
                OriginalCurrencyCode = transaction.OriginalCurrencyCode,
                ClaimCurrencyCode = transaction.ClaimCurrencyCode,
                TransactionAmountAccounting = transaction.TransactionAmountAccounting,
                TransactionAmountBase = transaction.TransactionAmountBase,
                TransactionAmountClaimCurrency = transaction.TransactionAmountClaimCurrency,
                TransactionAmountOriginal = transaction.TransactionAmountOriginal,
                MovementAmountAccounting = transaction.MovementAmountAccounting,
                MovementAmountBase = transaction.MovementAmountBase,
                MovementAmountClaimCurrency = transaction.MovementAmountClaimCurrency,
                MovementAmountOriginal = transaction.MovementAmountOriginal,
                ReserveType = (Xiap.Framework.Data.Claims.ReserveType)transaction.ReserveType,
                ReserveDate = transaction.ReserveDate,
                ReserveDaySequence = transaction.ReserveDaySequence
            };
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountDataWithReinsurance(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadUsersClaimPaymentAmountData(ClaimHeader claimHeader, SecurityUser userID, StaticValues.AuthorisationResult authorisationResult, FinancialAmountParticipationFilter filter)
        {
            throw new System.NotImplementedException();
        }


        public IEnumerable<ClaimFinancialAmount> ReadUsersClaimPaymentAmountData(ClaimHeader claimHeader, SecurityUser userID, StaticValues.AuthorisationResult authorisationResult, FinancialAmountParticipationFilter filter, ClaimTransactionHeader claimTransactionHeader)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountData(ClaimDetail claimDetail, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountDataWithReinsurance(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadClaimDetailAmountDataWithReinsurance(ClaimDetail claimDetail, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, bool includeEstimated, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadTransactionAmountData(ClaimTransactionHeader claimTransactionHeader, StaticValues.AmountType amountType, FinancialAmountParticipationFilter filter, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadUsersClaimPaymentAmountData(ClaimHeader claimHeader, SecurityUser userID, StaticValues.AuthorisationResult authorisationResult, FinancialAmountParticipationFilter filter, ClaimTransactionHeader claimTransactionHeader, bool? isIndemnityMovementExcluded, bool? isFeeMovementExcluded, bool? isRecoveryMovementExcluded, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ClaimFinancialAmount> ReadUsersClaimPaymentAmountData(ClaimHeader claimHeader, SecurityUser userID, StaticValues.AuthorisationResult authorisationResult, FinancialAmountParticipationFilter filter, ClaimTransactionHeader claimTransactionHeader, bool isExcludedFromCurrent = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
