namespace Xiap.DataMigration.GeniusInterface.AXACS.Entities
{
    using System;
    using System.Collections.Generic;

    public class FlattenedTransaction
    {
        public bool TransactionHeaderIsInProgress { get; set; }
        public bool TransactionGroupIsInProgress { get; set; }
        public Int64 ClaimHeaderID { get; set; }
        public Int64 ClaimTransactionHeaderID { get; set; }
        //public long? PayeeClaimNameInvolvementID { get; set; }
        //public long? PayerClaimNameInvolvementID { get; set; }
        //public long? AddresseeClaimNameInvolvementID { get; set; }
        //public long? NoteHeaderID { get; set; }
        //public long? ReceiptID { get; set; }
        //public long? CreatedByUserID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime TransactionDate { get; set; }
        public String ClaimTransactionHeaderReference { get; set; }
        public String ClaimTransactionDescription { get; set; }
        //public String ExternalTransactionIdentifier { get; set; }
        //public decimal? ExternalTransactionSequence { get; set; }
        public short? ClaimTransactionSource { get; set; }
        public short? PaymentAuthorisationStatus { get; set; }
        public short? ReserveAuthorisationStatus { get; set; }
        public decimal? PercentageAppliedToSourceAmount { get; set; }
        public bool? IsDisputedTransaction { get; set; }
        public short? SourceAmountEntryBasis { get; set; }

        public global::System.String HeaderExternalReference { get; set; }
        //public Nullable<global::System.Int64> ProductClaimTransactionID { get; set; }
        public Nullable<global::System.Int16> RecoveryReserveAuthorisationStatus { get; set; }
        public Nullable<global::System.Int16> RecoveryReceiptAuthorisationStatus { get; set; }
        public Nullable<global::System.Boolean> IsReservePendingPaymentAction { get; set; }
        public Nullable<global::System.Boolean> IsRecoveryReservePendingRecoveryReceiptAction { get; set; }
        public Nullable<global::System.Boolean> IsMultiClaimDetailTransaction { get; set; }
        public Nullable<global::System.Boolean> IsClaimPaymentCancelled { get; set; }



        public Int64 ClaimDetailID { get; set; }
        public String ClaimDetailReference { get; set; }
        public String OriginalCurrencyCode { get; set; }
        public String AccountingCurrencyCode { get; set; }
        public String ClaimCurrencyCode { get; set; }
        public decimal? OriginalToBaseExchangeRate { get; set; }
        public short? OriginalToBaseExchangeRateOperand { get; set; }
        public decimal? AccountingToBaseExchangeRate { get; set; }
        public short? AccountingToBaseExchangeRateOperand { get; set; }
        public decimal? ClaimToBaseExchangeRate { get; set; }
        public short? ClaimToBaseExchangeRateOperand { get; set; }
        public decimal? OrderPercent { get; set; }
        public decimal? LinePercent { get; set; }
        public decimal? SharePercent { get; set; }
        public String GroupExternalReference { get; set; }

        public global::System.Int64 ClaimTransactionDetailID { get; set; }
        public global::System.Int64 ClaimTransactionGroupID { get; set; }
        public Nullable<global::System.Decimal> CalculationSourceAmountOriginal { get; set; }
        public Nullable<global::System.Decimal> TransactionAmountOriginal { get; set; }
        public Nullable<global::System.Decimal> MovementAmountOriginal { get; set; }
        public Nullable<global::System.Decimal> TransactionAmountAccounting { get; set; }
        public Nullable<global::System.Decimal> MovementAmountAccounting { get; set; }
        public Nullable<global::System.Decimal> TransactionAmountClaimCurrency { get; set; }
        public Nullable<global::System.Decimal> MovementAmountClaimCurrency { get; set; }
        public Nullable<global::System.Decimal> TransactionAmountBase { get; set; }
        public Nullable<global::System.Decimal> MovementAmountBase { get; set; }
        public Nullable<global::System.Int16> OrderShareCoinsurance { get; set; }
        public Nullable<global::System.Int16> AmountType { get; set; }
        public global::System.String MovementType { get; set; }
        public Nullable<global::System.DateTime> ReserveDate { get; set; }
        public Nullable<global::System.Int16> ReserveDaySequence { get; set; }
        public Nullable<global::System.Int16> ReserveType { get; set; }
        //public global::System.String DetailExternalReference { get; set; }

        public IEnumerable<AuthorisationLog> AuthorisationLogs { get; set; }
    }
}