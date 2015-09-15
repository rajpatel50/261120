using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic.CustomDataUpdaters;
using Xiap.Claims.BusinessLogic.PluginPredicates;
using Xiap.Claims.BusinessTransaction;
using Xiap.Claims.Data;
using Xiap.Framework;
using Xiap.Framework.Data;
using Xiap.Framework.Entity;
using Xiap.Framework.Metadata;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic.CustomDataUpdaters
{
    public class AXAClaimHeaderDataUpdater : ClaimHeaderDataUpdater
    {
        private const string AXAClaimsDecisionTablePluginPredicate = "AXAClaimsDecisionTablePluginPredicate";
        private const string ClaimSummaryTotalsFieldsMotorProductCheck = "ClaimSummaryTotalsFieldsMotorProductCheck";

        private static void UpdateClaimHeaderDataForReopenTransaction(IBusinessComponent component, BusinessData dataClass)
        {
            ClaimHeader claimHeader = component as ClaimHeader;
            ClaimHeaderData headerData = dataClass as ClaimHeaderData;

            if (claimHeader != null && headerData != null && claimHeader.Context != null && claimHeader.Context.TransactionType == ClaimsProcessConstants.REOPENCLAIM)
            {
                if (claimHeader.HistoricalClaimTransactionHeaders.Any(a => a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Payment))
                {
                    headerData.HasPayment = true;
                }

                if (claimHeader.HistoricalClaimTransactionHeaders.Any(a => a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Reserve))
                {
                    headerData.HasReserve = true;
                }
            }
        }

        private static void UpdateCustomProperties(IBusinessComponent component, BusinessData dataClass)
        {
            ClaimHeader claimHeader = component as ClaimHeader;
            ClaimHeaderData headerData = dataClass as ClaimHeaderData;

            CodeRow code = claimHeader.CustomCode01Field.AllowedValues().Where(c => c.Code == claimHeader.CustomCode01).FirstOrDefault();
            if (code != null)
            {
                headerData.CustomProperties["AXA_CustomCode01UI"] = code.Description;
            }

            code = claimHeader.ClaimHeaderAnalysisCode05Field.AllowedValues().Where(c => c.Code == claimHeader.ClaimHeaderAnalysisCode05).FirstOrDefault();
            if (code != null)
            {
                headerData.CustomProperties["AXA_ClaimHeaderAnalysisCode05UI"] = code.Description;
            }

            code = claimHeader.ClaimHeaderAnalysisCode06Field.AllowedValues().Where(c => c.Code == claimHeader.ClaimHeaderAnalysisCode06).FirstOrDefault();
            if (code != null)
            {
                headerData.CustomProperties["AXA_ClaimHeaderAnalysisCode06UI"] = code.Description;
            }

            string[] predicates = { ConfigurationManager.AppSettings[AXAClaimsDecisionTablePluginPredicate] };

            ClaimsDecisionTablePluginPredicate cdtPredicate = ObjectFactory.Resolve<ClaimsDecisionTablePluginPredicate>();
            bool isMotorProduct = cdtPredicate.CanExecute((IBusinessComponent)claimHeader, ProcessInvocationPoint.Blank, ClaimSummaryTotalsFieldsMotorProductCheck, predicates);

            short? processingMethod = claimHeader.GetProduct().ProductClaimDefinition.ClaimHeaderAutomaticDeductibleProcessingMethod;

            headerData.CustomProperties["TotalClaimLossField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = true, Title = "Total", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["ExcessField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = isMotorProduct, Title = "Excess", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["OutstandingEstimateField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = true, Title = "O/S Estimate", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["OutstandingRecoveryEstimateField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = true, Title = "O/S Recovery Estimate", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["OutstandingULREstimateField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = isMotorProduct, Title = "O/S ULR Estimate", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["PaymentsInProgressField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = true, Title = "Payments in Progress", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["RecoveryInProgressField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = true, Title = "Recoveries in Progress", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["ULRField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = isMotorProduct, Title = "ULR in Progress", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["TotalPaymentsPaidField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = true, Title = "Total Payments", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["RecoveriesCompletedField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = true, Title = "Recoveries Completed", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["ULRCompletedField"] = new Field() { TypeInfo = new DecimalTypeInfo(), Readonly = true, Visible = isMotorProduct, Title = "ULR Completed", DefaultValue = 0, Precision = 15, Scale = 2 };
            headerData.CustomProperties["AXAManagedShareField"] = new Field { TypeInfo = new DecimalTypeInfo(), Precision = 10, Scale = 7, Visible = processingMethod.GetValueOrDefault() == (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.NotActive, Readonly = true };
            headerData.CustomProperties["AXAShareField"] = new Field { TypeInfo = new DecimalTypeInfo(), Precision = 10, Scale = 7, Visible = processingMethod.GetValueOrDefault() == (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.NotActive, Readonly = true };          

             using (var claimsEntities = ClaimsEntitiesFactory.GetClaimsEntities())
            {                
                var latestCTH = claimHeader.HistoricalClaimTransactionHeaders.OrderByDescending(h => h.ClaimTransactionHeaderID).FirstOrDefault();
                if (latestCTH != null && latestCTH.ClaimTransactionGroups.Any())
                {
                   var latestCTG= latestCTH.ClaimTransactionGroups.OrderByDescending(c => c.ClaimTransactionGroupID).FirstOrDefault();
                    if (latestCTG != null)
                    {
                        headerData.CustomProperties["CoinsuranceOfWholePercent"] = latestCTG.CoinsuranceOfWholePercent;
                        headerData.CustomProperties["ShareOfWholePercent"] = latestCTG.ShareOfWholePercent;
                    }
                }
                else
                {
                    headerData.CustomProperties["CoinsuranceOfWholePercent"] = null;
                    headerData.CustomProperties["ShareOfWholePercent"] = null;
                }
            }            
        }

        public override void UpdateBusinessData(IBusinessComponent component, BusinessData dataClass)
        {
            base.UpdateBusinessData(component, dataClass);
            this.UpdatePolicyReasonCodes(component, dataClass);
            UpdateClaimHeaderDataForReopenTransaction(component, dataClass);
            UpdateCustomProperties(component, dataClass);
        }

        private void UpdatePolicyReasonCodes(Xiap.Framework.IBusinessComponent component, BusinessData dataClass)
        {
            ClaimHeader claimHeader = component as ClaimHeader;
            ClaimHeaderData headerData = dataClass as ClaimHeaderData;
            if (claimHeader.Context.GetAttachedData<DeductibleReasonCode>(false) != null)
            {
                DeductibleReasonCode deductibleReasonCode = claimHeader.Context.GetAttachedData<DeductibleReasonCode>(true).FirstOrDefault();
                if (deductibleReasonCode.PolicyReasonCodes != null && deductibleReasonCode.PolicyReasonCodes.Count > 0)
                {
                    headerData.CodeRowList = deductibleReasonCode.PolicyReasonCodes;
                }
            }
        }       
    }
}
