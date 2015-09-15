namespace Xiap.DataMigration.GeniusInterface.AXACS.Entities
{
    using System;
    using System.Linq;
    using AutoMapper;
    using Claims.BusinessComponent;

    public class MapperConfiguration
    {
        public static void Initialize()
        {
            //Mapper.CreateMap<StagingClaimTransactionHeader, ClaimTransactionHeader>()
            //    .ForSourceMember(c => c.ClaimTransactionHeaderID, a => a.Ignore());
            //Mapper.CreateMap<StagingClaimTransactionGroup, ClaimTransactionGroup>()
            //    .ForSourceMember(c => c.ClaimTransactionHeaderID, a => a.Ignore())
            //    .ForSourceMember(c => c.ClaimTransactionGroupID, a => a.Ignore());
            //Mapper.CreateMap<StagingClaimTransactionDetail, ClaimTransactionDetail>()
            //    .ForSourceMember(c => c.ClaimTransactionGroupID, a => a.Ignore())
            //    .ForSourceMember(c => c.ClaimTransactionDetailID, a => a.Ignore());

            //Mapper.CreateMap<ClaimTransactionHeader, TransactionHeader>()
            //    .ForMember(dest => dest.ClaimHeaderID, opt => opt.MapFrom(src => src.ClaimHeader.ClaimHeaderID))
            //    .ForMember(dest => dest.PayeeClaimNameInvolvementID, opt => opt.MapFrom(src => src.PayeeClaimNameInvolvement.ClaimNameInvolvementID))
            //    .ForMember(dest => dest.PayerClaimNameInvolvementID, opt => opt.MapFrom(src => src.PayerClaimNameInvolvement.ClaimNameInvolvementID))
            //    .ForMember(dest => dest.TransactionGroups, opt => opt.MapFrom(src => src.ClaimTransactionGroups));

            ////Mapper.CreateMap<ClaimTransactionAuthorisationLog, StagingClaimTransactionAuthorisationLog>()
            ////    .ForMember(dest => dest.ActionedByUserID, opt => opt.MapFrom(src => src.ActionedByUserID));

            //Mapper.CreateMap<ClaimTransactionGroup, TransactionGroup>()
            //    .ForMember(dest => dest.ClaimTransactionHeaderID, opt => opt.MapFrom(src => src.ClaimTransactionHeader.ClaimTransactionHeaderID))
            //    .ForMember(dest => dest.ClaimDetailID, opt => opt.MapFrom(src => src.ClaimDetail.ClaimDetailID))
            //    .ForMember(dest => dest.TransactionDetails, opt => opt.MapFrom(src => src.ClaimTransactionDetails));

            //Mapper.CreateMap<ClaimTransactionDetail, TransactionDetail>()
            //    .ForMember(dest => dest.ClaimTransactionGroupID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionGroupID))
            //    .ForMember(dest => dest.ClaimDetailReference, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimDetail.ClaimDetailReference))
            //    .ForMember(dest => dest.ClaimCurrencyCode, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimCurrencyCode));

            Mapper.CreateMap<ClaimTransactionDetail, FlattenedTransaction>()
                .ForMember(dest => dest.TransactionHeaderIsInProgress, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.IsInProgress))
                .ForMember(dest => dest.TransactionGroupIsInProgress, opt => opt.MapFrom(src => src.ClaimTransactionGroup.IsInProgress))
                .ForMember(dest => dest.ClaimDetailID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimDetail.ClaimDetailID))
                .ForMember(dest => dest.ClaimDetailReference, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimDetail.ClaimDetailReference))
                
                .ForMember(dest => dest.CalculationSourceAmountOriginal, opt => opt.MapFrom(src => src.CalculationSourceAmountOriginal))
                .ForMember(dest => dest.MovementAmountAccounting, opt => opt.MapFrom(src => src.MovementAmountAccounting))
                .ForMember(dest => dest.MovementAmountBase, opt => opt.MapFrom(src => src.MovementAmountBase))
                .ForMember(dest => dest.MovementAmountClaimCurrency, opt => opt.MapFrom(src => src.MovementAmountClaimCurrency))
                .ForMember(dest => dest.MovementAmountOriginal, opt => opt.MapFrom(src => src.MovementAmountOriginal))
                .ForMember(dest => dest.TransactionAmountAccounting, opt => opt.MapFrom(src => src.TransactionAmountAccounting))
                .ForMember(dest => dest.TransactionAmountBase, opt => opt.MapFrom(src => src.TransactionAmountBase))
                .ForMember(dest => dest.TransactionAmountClaimCurrency, opt => opt.MapFrom(src => src.TransactionAmountClaimCurrency))
                .ForMember(dest => dest.TransactionAmountOriginal, opt => opt.MapFrom(src => src.TransactionAmountOriginal))
                //.ForMember(dest => dest.DetailExternalReference, opt => opt.MapFrom(src => src.ExternalReference))
                

                .ForMember(dest => dest.OriginalCurrencyCode, opt => opt.MapFrom(src => src.ClaimTransactionGroup.OriginalCurrencyCode))
                .ForMember(dest => dest.AccountingCurrencyCode, opt => opt.MapFrom(src => src.ClaimTransactionGroup.AccountingCurrencyCode))
                .ForMember(dest => dest.ClaimCurrencyCode, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimCurrencyCode))
                .ForMember(dest => dest.OriginalToBaseExchangeRate, opt => opt.MapFrom(src => src.ClaimTransactionGroup.OriginalToBaseExchangeRate))
                .ForMember(dest => dest.OriginalToBaseExchangeRateOperand, opt => opt.MapFrom(src => src.ClaimTransactionGroup.OriginalToBaseExchangeRateOperand))
                .ForMember(dest => dest.AccountingToBaseExchangeRate, opt => opt.MapFrom(src => src.ClaimTransactionGroup.AccountingToBaseExchangeRate))
                .ForMember(dest => dest.AccountingToBaseExchangeRateOperand, opt => opt.MapFrom(src => src.ClaimTransactionGroup.AccountingToBaseExchangeRateOperand))
                .ForMember(dest => dest.ClaimToBaseExchangeRate, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimToBaseExchangeRate))
                .ForMember(dest => dest.ClaimToBaseExchangeRateOperand, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimToBaseExchangeRateOperand))
                .ForMember(dest => dest.OrderPercent, opt => opt.MapFrom(src => src.ClaimTransactionGroup.OrderOfWholePercent))
                //.ForMember(dest => dest.LinePercent, opt => opt.MapFrom(src => src.ClaimTransactionGroup.LinePercent))
                .ForMember(dest => dest.SharePercent, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ShareOfWholePercent))
                .ForMember(dest => dest.GroupExternalReference, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ExternalReference))

                .ForMember(dest => dest.ClaimHeaderID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ClaimHeader.ClaimHeaderID))
                //.ForMember(dest => dest.PayeeClaimNameInvolvementID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.PayeeClaimNameInvolvement.PID))
                //.ForMember(dest => dest.PayerClaimNameInvolvementID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.PayerClaimNameInvolvementID))
                //.ForMember(dest => dest.AddresseeClaimNameInvolvementID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.AddresseeClaimNameInvolvementID))
                //.ForMember(dest => dest.NoteHeaderID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.NoteHeaderID))
                //.ForMember(dest => dest.ReceiptID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ReceiptID))
                //.ForMember(dest => dest.CreatedByUserID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.CreatedByUserID))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.CreatedDate.GetValueOrDefault(DateTime.MinValue)))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.TransactionDate.GetValueOrDefault(DateTime.MinValue)))
                
                .ForMember(dest => dest.ClaimTransactionHeaderReference, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ClaimTransactionHeaderReference))
                .ForMember(dest => dest.PaymentAuthorisationStatus, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.PaymentAuthorisationStatus))
                .ForMember(dest => dest.ReserveAuthorisationStatus, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ReserveAuthorisationStatus))
                .ForMember(dest => dest.PercentageAppliedToSourceAmount, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.PercentageAppliedToSourceAmount))
                .ForMember(dest => dest.IsDisputedTransaction, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.IsDisputedTransaction))
                .ForMember(dest => dest.SourceAmountEntryBasis, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.SourceAmountEntryBasis))

                .ForMember(dest => dest.HeaderExternalReference, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ExternalReference))
                //.ForMember(dest => dest.ProductClaimTransactionID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ProductClaimTransactionID))
                .ForMember(dest => dest.RecoveryReserveAuthorisationStatus, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.RecoveryReserveAuthorisationStatus))
                .ForMember(dest => dest.RecoveryReceiptAuthorisationStatus, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.RecoveryReceiptAuthorisationStatus))
                .ForMember(dest => dest.IsReservePendingPaymentAction, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.IsReservePendingPaymentAction))
                .ForMember(dest => dest.IsRecoveryReservePendingRecoveryReceiptAction, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.IsRecoveryReservePendingRecoveryReceiptAction))
                .ForMember(dest => dest.IsMultiClaimDetailTransaction, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.IsMultiClaimDetailTransaction))
                .ForMember(dest => dest.IsClaimPaymentCancelled, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.IsClaimPaymentCancelled))
                .ForMember(dest => dest.ClaimTransactionHeaderID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ClaimTransactionHeaderID))
                .ForMember(dest => dest.ClaimTransactionSource, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ClaimTransactionSource))
                .ForMember(dest => dest.ClaimTransactionGroupID, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionGroupID))

                .ForMember(dest => dest.ClaimTransactionDescription, opt => opt.MapFrom(src => src.ClaimTransactionGroup.ClaimTransactionHeader.ClaimTransactionDescription))

                .ForMember(dest => dest.AuthorisationLogs, opt => opt.MapFrom(src =>
                    src.ClaimTransactionGroup.ClaimTransactionHeader.ClaimTransactionAuthorisationLogs.Select(l => Mapper.Map<ClaimTransactionAuthorisationLog, AuthorisationLog>(l))))
                ;

            Mapper.CreateMap<ClaimTransactionAuthorisationLog, AuthorisationLog>()
                .ForMember(dest => dest.ActionedByUserID, opt => opt.MapFrom(src => src.ActionedByUserID))
                .ForMember(dest => dest.AmountType, opt => opt.MapFrom(src => src.AmountType))
                .ForMember(dest => dest.AuthorisationResult, opt => opt.MapFrom(src => src.AuthorisationResult))
                ;
            //         public Int64 ClaimTransactionHeaderID { get; set; }
            //public long? PayeeClaimNameInvolvementID { get; set; }
            //public long? PayerClaimNameInvolvementID { get; set; }
            //public long? AddresseeClaimNameInvolvementID { get; set; }
            //public long? NoteHeaderID { get; set; }
            //public long? ReceiptID { get; set; }
            //public long? CreatedByUserID { get; set; }
            //public DateTime? CreatedDate { get; set; }
            //public DateTime? TransactionDate { get; set; }
            //public String ClaimTransactionHeaderReference { get; set; }
            //public String ClaimTransactionDescription { get; set; }
            //public String ExternalTransactionIdentifier { get; set; }
            //public decimal? ExternalTransactionSequence { get; set; }
            //public short? ClaimTransactionSource { get; set; }
            //public short? PaymentAuthorisationStatus { get; set; }
            //public short? ReserveAuthorisationStatus { get; set; }
            //public decimal? PercentageAppliedToSourceAmount { get; set; }
            //public bool? IsDisputedTransaction { get; set; }
            //public short? SourceAmountEntryBasis { get; set; }

            //    public global::System.String ExternalReference { get; set; }
            //public Nullable<global::System.Int64> ProductClaimTransactionID { get; set; }
            //public Nullable<global::System.Int16> RecoveryReserveAuthorisationStatus { get; set; }
            //public Nullable<global::System.Int16> RecoveryReceiptAuthorisationStatus { get; set; }
            //public Nullable<global::System.Boolean> IsReservePendingPaymentAction { get; set; }
            //public Nullable<global::System.Boolean> IsRecoveryReservePendingRecoveryReceiptAction { get; set; }
            //public Nullable<global::System.Boolean> IsMultiClaimDetailTransaction { get; set; }
            //public Nullable<global::System.Boolean> IsClaimPaymentCancelled { get; set; }
        }
    }
}
