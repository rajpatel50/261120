using AutoMapper;
using Xiap.DataMigration.GeniusInterface.AXACS.Entities;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Claims.BusinessComponent;
    using Claims.BusinessLogic;
    using Claims.BusinessLogic.AuthorityCheck;
    using Claims.BusinessLogic.AuthorityCheck.Calculation;
    using Framework;
    using Framework.Common;
    using Framework.Logging;
    using Framework.Messages;
    using Metadata.BusinessComponent;
    using Metadata.Data.Enums;
    using Newtonsoft.Json.Linq;
    using Xiap.Framework.Data.Claims;
    using ClaimDetail = Claims.BusinessComponent.ClaimDetail;

    public class CalculateDeductiblesHandler
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool Calculate(ClaimTransactionHeader claimTransactionHeader)
        {
            var transactionGroupCount = claimTransactionHeader.ClaimTransactionGroups.Count;

            if (transactionGroupCount == 0)
            {
                _Logger.Warn(string.Format("There are no transaction groups for this transaction header\r\n{0}",
                    JObject.FromObject(new{claimTransactionHeader.ClaimHeader.ClaimReference, claimTransactionHeader.ClaimTransactionHeaderReference})));
                return false;
            }

            if (transactionGroupCount > 1)
            {
                _Logger.Warn(string.Format("There is more than one transaction group for this transaction header\r\n{0}",
                    JObject.FromObject(new{claimTransactionHeader.ClaimHeader.ClaimReference, claimTransactionHeader.ClaimTransactionHeaderReference})));
                return false;
            }

            var claimHeader = claimTransactionHeader.ClaimHeader;
            var claimTransactionGroup = claimTransactionHeader.ClaimTransactionGroups.Single();
            var claimDetail = claimTransactionGroup.ClaimDetail;
            var user = claimHeader.Context.GetUser(Xiap.Framework.Security.XiapSecurity.GetUser().UserID);
            var financialContext = new CalculateDeductiblesHandler.FinancialTransactionContext(claimHeader, claimDetail, claimTransactionHeader, claimTransactionGroup, user);
            var result = GenerateDeductibles(financialContext);
            if (!result.Success)
            {
                return false;
            }

            ResetAmoundChangedFlag(claimTransactionHeader);

            if (_Logger.IsDebugEnabled)
            {
                PrintFinancialTransaction(claimTransactionHeader);
            }

            return true;
        }

        /// <summary>
        /// Creates deductibles (claim transaction details) where required for the new reserve/payment/recovery reserve/recovery receipt transaction
        /// </summary>
        /// <param name="financialContext">wrapper for context variables</param>
        /// <returns>Deductible Result</returns>
        private static DeductibleResult GenerateDeductibles(FinancialTransactionContext financialContext)
        {
            IDictionary<string, ProductClaimDetail> claimDetailProductMap = MapClaimDetailRefToProductClaimDetail(financialContext.ClaimHeader.ClaimDetails, financialContext.ProductClaimDetails);
            var currentProductClaimDetail = claimDetailProductMap[financialContext.ClaimDetail.ClaimDetailReference];

            bool isCalculatingExcess = IsCalculatingExcess(currentProductClaimDetail, financialContext.ClaimDetail);
            bool isCalculatingDeductibles = IsCalculatingDeductibles(currentProductClaimDetail, financialContext.ProductClaimDefinition, financialContext.ClaimHeader);



            if (isCalculatingExcess)
            {
                var deductibles = ResolveDeductibles(financialContext, currentProductClaimDetail);
                IEnumerable<ClaimFinancialAmount> reserves, payments, recoveryReserves;
                LoadFinancialAmounts(financialContext.TransactionSource, financialContext.ProductClaimDefinition, financialContext.ClaimDetail, financialContext.ClaimTransactionHeader, out reserves, out payments, out recoveryReserves);
                return CalculateDeductibles(financialContext.ProductClaimDefinition, financialContext, deductibles, reserves, payments, recoveryReserves);
            }
            else if (isCalculatingDeductibles)
            {
                var deductibles = ResolveDeductibles(financialContext);
                IEnumerable<ClaimFinancialAmount> reserves, payments, recoveryReserves;
                LoadFinancialAmounts(financialContext.TransactionSource, financialContext.ProductClaimDefinition, claimDetailProductMap, financialContext.ClaimHeader, financialContext.ClaimTransactionHeader, out reserves, out payments, out recoveryReserves);
                return CalculateDeductibles(financialContext.ProductClaimDefinition, financialContext, deductibles, reserves, payments, recoveryReserves);
            }

            return new DeductibleResult { Success = true };
        }

        /// <summary>
        /// For each deductible associated with this transaction we need to calculate how the latest transaction has affected the the current deductibles and generate any new deductibles as required
        /// </summary>
        /// <param name="productClaimDefinition">calim product</param>
        /// <param name="financialContext">context variables</param>
        /// <param name="deductibleDefinitions">deductibles associated with this transaction</param>
        /// <param name="reserves">historical reserves</param>
        /// <param name="payments">historical payments</param>
        /// <param name="recoveryReserves">recovery reserves</param>
        /// <returns>Deductible Result</returns>
        private static DeductibleResult CalculateDeductibles(ProductClaimDefinition productClaimDefinition, FinancialTransactionContext financialContext, IEnumerable<DeductibleDefinition> deductibleDefinitions, IEnumerable<ClaimFinancialAmount> reserves, IEnumerable<ClaimFinancialAmount> payments, IEnumerable<ClaimFinancialAmount> recoveryReserves)
        {
            var deductibleResult = new DeductibleResult { Success = true };
            var deductibleMovementTypes = deductibleDefinitions.SelectMany(a => a.GetMovementTypes());
            var recoveryDeductibleMovementTypes = deductibleDefinitions.SelectMany(a => a.GetRecoveryMovementTypes());


            var claimDetailRefs = financialContext.ClaimHeader.ClaimDetails.Select(a => a.ClaimDetailReference);
            var deductibleReserveContext = new DeductibleReserveCapacity(claimDetailRefs, reserves, deductibleMovementTypes);
            var deductibleRecoveryReserveContext = new DeductibleReserveCapacity(claimDetailRefs, recoveryReserves, recoveryDeductibleMovementTypes);

            bool isRecovery = IsRecovery(financialContext.TransactionSource);
            decimal sumOfLowerDeductibles = 0;
            decimal totalPaid = payments.Where(a => !deductibleMovementTypes.Contains(a.MovementType)).Sum(a => a.TransactionAmountClaimCurrency.GetValueOrDefault());
            decimal totalIncurred = CalculateTotalIncurred(productClaimDefinition, totalPaid, reserves.Where(a => !deductibleMovementTypes.Contains(a.MovementType)));
            decimal totalRecoveryIncurred = totalIncurred + recoveryReserves.Where(a => !recoveryDeductibleMovementTypes.Contains(a.MovementType)).Sum(a => a.TransactionAmountClaimCurrency.GetValueOrDefault(0));


            foreach (var deductible in deductibleDefinitions)
            {
                decimal deductiblePaid = CalculateDeductiblePayment(deductible, totalPaid, sumOfLowerDeductibles, payments);
                if (deductiblePaid != 0)
                {
                    var amountType = isRecovery ? StaticValues.AmountType.RecoveryReceipt : StaticValues.AmountType.Payment;
                    AddClaimTransactionDetails(financialContext.ClaimTransactionHeader.TransactionDate.GetValueOrDefault(DateTime.MinValue), financialContext.ClaimTransactionGroup, amountType, deductible, deductiblePaid);
                }

                if (isRecovery && deductible.RecoveryNonFundedMovementType != null)
                {
                    ClaimFinancialAmount latestReserve = LatestReserveOrNull(recoveryReserves, financialContext, deductible.RecoveryNonFundedMovementType);
                    decimal deductibleRecoveryReserve = CalculateDeductibleRecoveryReserves(deductible, latestReserve, totalIncurred, totalRecoveryIncurred, sumOfLowerDeductibles, recoveryReserves);
                    // although a deductible recovery reserve is a negative amount we want to keep it as a positive for now so we can apply deductible reserve adjustments
                    deductibleRecoveryReserve = -deductibleRecoveryReserve;
                    var reserveAdjResult = ApplyDeductibleReserveAdjustments(deductible.RecoveryNonFundedMovementType, deductible, deductibleRecoveryReserveContext, financialContext, recoveryReserves, latestReserve, deductibleRecoveryReserve);
                    deductibleRecoveryReserveContext.AppendToLowerDeductibles(deductible.RecoveryNonFundedMovementType);

                    if (!reserveAdjResult.Success)
                    {
                        deductibleResult = reserveAdjResult;
                    }
                }

                if (financialContext.TransactionSource != StaticValues.ClaimTransactionSource.RecoveryReserve)
                {
                    ClaimFinancialAmount latestReserve = LatestReserveOrNull(reserves, financialContext, deductible.NonFundedMovementType);
                    decimal deductibleReserve = CalculateDeductibleReserves(deductible, latestReserve, totalPaid, totalIncurred, sumOfLowerDeductibles, reserves);
                    var reserveAdjResult = ApplyDeductibleReserveAdjustments(deductible.NonFundedMovementType, deductible, deductibleReserveContext, financialContext, reserves, latestReserve, deductibleReserve);
                    deductibleReserveContext.AppendToLowerDeductibles(deductible.NonFundedMovementType);

                    if (!reserveAdjResult.Success)
                    {
                        deductibleResult = reserveAdjResult;
                    }
                }



                sumOfLowerDeductibles += deductible.Amount;
            }

            return deductibleResult;
        }

        private static decimal CalculateDeductiblePayment(DeductibleDefinition deductibleDefinition, decimal totalPaid, decimal sumOfLowerDeductibles, IEnumerable<ClaimFinancialAmount> payments)
        {
            string deductibleMovementType = deductibleDefinition.NonFundedMovementType;
            decimal deductibleAmount = deductibleDefinition.Amount;
            decimal deductiblePaidToDate = -CalculateSum(payments, deductibleMovementType);

            // calculate payments
            decimal deductibleTotalPaid = Math.Min(deductibleAmount, Math.Max(totalPaid - sumOfLowerDeductibles, 0));
            decimal deductiblePaid = deductibleTotalPaid - deductiblePaidToDate;

            return deductiblePaid;
        }

        private static decimal CalculateDeductibleReserves(DeductibleDefinition deductibleDefinition, ClaimFinancialAmount latestReserve, decimal totalPaid, decimal totalIncurred, decimal sumOfLowerDeductibles, IEnumerable<ClaimFinancialAmount> reserves)
        {
            string deductibleMovementType = deductibleDefinition.NonFundedMovementType;
            decimal deductibleAmount = deductibleDefinition.Amount;
            decimal deductibleTotalPaid = Math.Min(deductibleAmount, Math.Max(totalPaid - sumOfLowerDeductibles, 0));

            // supersede latest if entering a new reserve
            if (latestReserve != null)
            {
                reserves = reserves.Except(new[] { latestReserve });
            }

            decimal sumOfDeductibleReserves = -CalculateSum(reserves, deductibleMovementType);

            // calculate reserves
            decimal deductibleTotalIncurred = Math.Min(deductibleAmount, Math.Max(totalIncurred - sumOfLowerDeductibles, 0));
            decimal deductibleTotalReserve = deductibleTotalIncurred - deductibleTotalPaid;

            decimal deductibleReserve = deductibleTotalReserve - sumOfDeductibleReserves;
            return deductibleReserve;
        }

        private static decimal CalculateDeductibleRecoveryReserves(DeductibleDefinition deductibleDefinition, ClaimFinancialAmount latestRecoveryReserve, decimal totalIncurred, decimal totalRecoveryIncurred, decimal sumOfLowerDeductibles, IEnumerable<ClaimFinancialAmount> recoveryReserves)
        {
            string deductibleMovementType = deductibleDefinition.RecoveryNonFundedMovementType;
            decimal deductibleAmount = deductibleDefinition.Amount;

            // supersede latest if entering a new reserve
            if (latestRecoveryReserve != null)
            {
                recoveryReserves = recoveryReserves.Except(new[] { latestRecoveryReserve });
            }

            decimal sumOfDeductibleReserves = -CalculateSum(recoveryReserves, deductibleMovementType);

            decimal deductibleTotalIncurred = Math.Min(deductibleAmount, Math.Max(totalIncurred - sumOfLowerDeductibles, 0));
            decimal deductibleTotalRecoveryIncurred = Math.Min(deductibleAmount, Math.Max(totalRecoveryIncurred - sumOfLowerDeductibles, 0));
            decimal deductibleTotalRecoveryReserve = deductibleTotalRecoveryIncurred - deductibleTotalIncurred;

            decimal deductibleRecoveryReserve = deductibleTotalRecoveryReserve - sumOfDeductibleReserves;

            return deductibleRecoveryReserve;
        }

        private static DeductibleResult ApplyDeductibleReserveAdjustments(string movementType, DeductibleDefinition deductibleDefinition, DeductibleReserveCapacity deductibleReserveContext, FinancialTransactionContext financialContext, IEnumerable<ClaimFinancialAmount> reserves, ClaimFinancialAmount latestReserve, decimal reserveAdjustment)
        {
            string claimDetailRef = financialContext.ClaimDetail.ClaimDetailReference;
            decimal incurredTotal = deductibleReserveContext.ResolveCapacity(claimDetailRef);
            decimal revisedAmount = Math.Max(Math.Min(incurredTotal, reserveAdjustment), 0);

            // if it's a recovery receipt transaction and there is reserve adjustment, stop.
            if (IsRecovery(financialContext.TransactionSource)
                && movementType == deductibleDefinition.NonFundedMovementType
                && ((latestReserve == null && reserveAdjustment != 0) || (latestReserve != null && reserveAdjustment != latestReserve.TransactionAmountClaimCurrency)))
            {
                return new DeductibleResult { Success = false, Message = MessageConstants.RecoveryReservesRequireManualReview };
            }

            reserveAdjustment -= AddReserve(revisedAmount, financialContext, financialContext.ClaimDetail, financialContext.ClaimTransactionGroup, deductibleDefinition, deductibleReserveContext, latestReserve);

            // if the reserve adjustment couldn't be absorbed by the current claim detail apply accross the claim
            if (reserveAdjustment != 0 && !deductibleDefinition.IsClaimDetailDeductible)
            {
                // don't process if currencies aren't all the same
                if (!AreReservesInSameCurrency(reserves, movementType))
                {
                    return new DeductibleResult { Success = false, Message = MessageConstants.ReservesRequireManualReview };
                }

                var reservesForMovementType = reserves.Where(a => a.MovementType == movementType);
                var claimDetails = from claimDetail in financialContext.ClaimHeader.ClaimDetails
                                   join reserve in reservesForMovementType on claimDetail.ClaimDetailReference equals reserve.ClaimDetailReference into cr
                                   from groupData in cr.DefaultIfEmpty()
                                   where claimDetail.ClaimDetailReference != claimDetailRef
                                   orderby groupData != null ? Math.Abs(groupData.TransactionAmountClaimCurrency.GetValueOrDefault()) : 0 descending
                                   select claimDetail;


                foreach (var claimDetail in claimDetails)
                {
                    incurredTotal = deductibleReserveContext.ResolveCapacity(claimDetail.ClaimDetailReference);
                    ClaimFinancialAmount remainingAmount = reserves.SingleOrDefault(a => a.ClaimDetailReference == claimDetail.ClaimDetailReference && a.MovementType == movementType);
                    decimal remainingAbsAmount = remainingAmount != null ? Math.Abs(remainingAmount.TransactionAmountClaimCurrency.GetValueOrDefault()) : 0;
                    revisedAmount = Math.Max(Math.Min(incurredTotal, remainingAbsAmount + reserveAdjustment), 0);

                    var newClaimTransactionGroup = ResolveClaimTransactionGroup(financialContext.ClaimTransactionHeader, financialContext.ClaimTransactionGroup, claimDetail);
                    revisedAmount = AddReserve(revisedAmount, financialContext, claimDetail, newClaimTransactionGroup, deductibleDefinition, deductibleReserveContext, remainingAmount);
                    reserveAdjustment -= revisedAmount - remainingAbsAmount;

                    if (reserveAdjustment == 0)
                    {
                        break;
                    }
                }
            }

            return new DeductibleResult { Success = true };
        }


        private static ClaimTransactionGroup ResolveClaimTransactionGroup(ClaimTransactionHeader claimTransactionHeader, ClaimTransactionGroup currentClaimTransactionGroup, ClaimDetail claimDetail)
        {
            var claimTransactionGroup = claimTransactionHeader.ClaimTransactionGroups.SingleOrDefault(a => a.ClaimDetail == claimDetail);
            if (claimTransactionGroup == null)
            {
                claimTransactionGroup = claimTransactionHeader.AddNewClaimTransactionGroup(claimDetail, false);
                claimTransactionGroup.UpdateData(currentClaimTransactionGroup);
                // don't want to copy the id accross
                claimTransactionGroup.ClaimTransactionGroupID = 0;
            }

            return claimTransactionGroup;
        }


        private static decimal AddReserve(decimal amount, FinancialTransactionContext financialContext, ClaimDetail claimDetail, ClaimTransactionGroup claimTransactionGroup, DeductibleDefinition deductibleDefinition, DeductibleReserveCapacity deductibleReserveContext, ClaimFinancialAmount latestReserve = null)
        {
            StaticValues.AmountType amountType;
            string nonFundedMovementType;
            if (IsRecovery(financialContext.TransactionSource))
            {
                amountType = StaticValues.AmountType.RecoveryReserve;
                nonFundedMovementType = deductibleDefinition.RecoveryNonFundedMovementType;
            }
            else
            {
                amountType = StaticValues.AmountType.Reserve;
                nonFundedMovementType = deductibleDefinition.NonFundedMovementType;
            }


            if (latestReserve == null)
            {
                if (amount > 0)
                {
                    AddClaimTransactionDetails(financialContext.ClaimTransactionHeader.TransactionDate.GetValueOrDefault(DateTime.MinValue), claimTransactionGroup, amountType, deductibleDefinition, amount, latestReserve);
                    deductibleReserveContext.AdjustDeductibleAttached(claimDetail.ClaimDetailReference, nonFundedMovementType, amount);
                }
                else
                {
                    amount = 0;
                }
            }
            else if (amount != Math.Abs(latestReserve.TransactionAmountClaimCurrency.GetValueOrDefault(0)))
            {
                AddClaimTransactionDetails(financialContext.ClaimTransactionHeader.TransactionDate.GetValueOrDefault(DateTime.MinValue), claimTransactionGroup, amountType, deductibleDefinition, amount, latestReserve);
                deductibleReserveContext.AdjustDeductibleAttached(claimDetail.ClaimDetailReference, nonFundedMovementType, amount + latestReserve.TransactionAmountClaimCurrency.GetValueOrDefault());
            }

            return amount;
        }

        private static decimal CalculateTotalIncurred(ProductClaimDefinition productClaimDefinition, decimal totalPaid, IEnumerable<ClaimFinancialAmount> reserves)
        {
            if (productClaimDefinition.IncurredAmountDerivationMethod != (short)StaticValues.IncurredAmountDerivationMethod.PaymentsOnly)
            {
                totalPaid += reserves.Sum(a => a.TransactionAmountClaimCurrency.GetValueOrDefault(0));
            }

            return totalPaid;
        }

        private static decimal CalculateSum(IEnumerable<ClaimFinancialAmount> amounts, string movementType)
        {
            return amounts.Where(a => a.MovementType == movementType).Sum(a => a.TransactionAmountClaimCurrency.GetValueOrDefault());
        }

        private static bool IsCalculatingExcess(ProductClaimDetail productClaimDetail, ClaimDetail claimDetail)
        {
            return productClaimDetail.ClaimDetailAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible
                && claimDetail.IsAutomaticDeductibleProcessingApplied == true;
        }

        private static bool IsCalculatingDeductibles(ProductClaimDetail productClaimDetail, ProductClaimDefinition productClaimDefinition, ClaimHeader claimHeader)
        {
            return productClaimDetail.ClaimDetailAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.FromClaimHeader
                && productClaimDefinition.ClaimHeaderAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.StandardClaimHeaderDeductible
                && claimHeader.IsAutomaticDeductibleProcessingApplied == true;
        }

        private static bool AreReservesInSameCurrency(IEnumerable<ClaimFinancialAmount> reserves, string movementType)
        {
            return (from reserve in reserves
                    where reserve.MovementType == movementType
                    select reserve.OriginalCurrencyCode)
                   .Distinct()
                   .Count() <= 1;
        }

        private static IDictionary<string, ProductClaimDetail> MapClaimDetailRefToProductClaimDetail(IEnumerable<ClaimDetail> claimDetails, IEnumerable<ProductClaimDetail> productClaimDetails)
        {
            return (from pcd in productClaimDetails
                    join cd in claimDetails on pcd.ProductClaimDetailID equals cd.ProductClaimDetailID
                    select new { cd = cd, pcd = pcd })
                   .ToDictionary(a => a.cd.ClaimDetailReference, a => a.pcd);
        }

        private static IEnumerable<DeductibleDefinition> ResolveDeductibles(FinancialTransactionContext context)
        {
            var productClaimDefinition = context.ProductClaimDefinition;
            var claim = context.ClaimHeader;
            var productCode = context.Product.Product.Code;
            var definitions = new List<DeductibleDefinition>();
            var user = context.CurrentUser;
            var claimTransactionContext = claim.Context;
            CreateDeductibleDefinitionIfEnabled(claimTransactionContext, false, definitions, claim.PolicyDeductible01, claim.IsDeductible01PaidByInsurer, productClaimDefinition.AutomaticDeductible01MovementTypeCode, productClaimDefinition.InsurerFundedDeductible01MovementTypeCode, productCode, user);
            CreateDeductibleDefinitionIfEnabled(claimTransactionContext, false, definitions, claim.PolicyDeductible02, claim.IsDeductible02PaidByInsurer, productClaimDefinition.AutomaticDeductible02MovementTypeCode, productClaimDefinition.InsurerFundedDeductible02MovementTypeCode, productCode, user);
            CreateDeductibleDefinitionIfEnabled(claimTransactionContext, false, definitions, claim.PolicyDeductible03, claim.IsDeductible03PaidByInsurer, productClaimDefinition.AutomaticDeductible03MovementTypeCode, productClaimDefinition.InsurerFundedDeductible03MovementTypeCode, productCode, user);
            CreateDeductibleDefinitionIfEnabled(claimTransactionContext, false, definitions, claim.PolicyDeductible04, claim.IsDeductible04PaidByInsurer, productClaimDefinition.AutomaticDeductible04MovementTypeCode, productClaimDefinition.InsurerFundedDeductible04MovementTypeCode, productCode, user);
            CreateDeductibleDefinitionIfEnabled(claimTransactionContext, false, definitions, claim.PolicyDeductible05, claim.IsDeductible05PaidByInsurer, productClaimDefinition.AutomaticDeductible05MovementTypeCode, productClaimDefinition.InsurerFundedDeductible05MovementTypeCode, productCode, user);



            return definitions;
        }

        private static IEnumerable<DeductibleDefinition> ResolveDeductibles(FinancialTransactionContext context, ProductClaimDetail productClaimDetail)
        {
            var claimDetail = context.ClaimDetail;
            var productCode = context.Product.Product.Code;
            var definitions = new List<DeductibleDefinition>();
            var user = context.CurrentUser;
            var claimTransactionContext = claimDetail.Context;
            CreateDeductibleDefinitionIfEnabled(claimTransactionContext, true, definitions, claimDetail.PolicyDeductible01, claimDetail.IsDeductible01PaidByInsurer, productClaimDetail.AutomaticDeductible01MovementTypeCode, productClaimDetail.InsurerFundedDeductible01MovementTypeCode, productCode, user);

            return definitions;
        }

        private static void CreateDeductibleDefinitionIfEnabled(TransactionContext transactionContext, bool isClaimDetailLevelDeductible, List<DeductibleDefinition> deductibles, decimal? amount, bool? isDeductiblePaidByInsurer, string nonFundedMovementType, string fundedMovementType, string productCode, IBusinessComponent context)
        {
            if (!amount.HasValue)
            {
                return;
            }

            var deductibleDefinition = new DeductibleDefinition { Amount = amount.Value, NonFundedMovementType = nonFundedMovementType };
            deductibleDefinition.IsClaimDetailDeductible = isClaimDetailLevelDeductible;

            if (isDeductiblePaidByInsurer.GetValueOrDefault(false))
            {
                if (string.IsNullOrEmpty(fundedMovementType))
                {
                    throw new InvalidOperationException(MessageServiceFactory.GetMessageBody(MessageConstants.NoFundedMovementType, nonFundedMovementType));
                }

                deductibleDefinition.FundedMovementType = fundedMovementType;
            }

            PopulateRecoveryMovementType(transactionContext, deductibleDefinition, productCode, context);
            deductibles.Add(deductibleDefinition);
        }

        /// <summary>
        /// Load historical reserves/payments/recovery reserves/recovery receipts for the specified claim
        /// </summary>
        /// <param name="transactionSource">transaction source</param>
        /// <param name="productClaimDefinition">claim product</param>
        /// <param name="claimDetailProductMap">claim details used for filtering</param>
        /// <param name="claimHeader">claim header</param>
        /// <param name="reserves">resulting reserves</param>
        /// <param name="payments">resulting payments and recovery receipts</param>
        /// <param name="recoveryReserves">resulting recovery reserves</param>
        private static void LoadFinancialAmounts(StaticValues.ClaimTransactionSource transactionSource, ProductClaimDefinition productClaimDefinition, IDictionary<string, ProductClaimDetail> claimDetailProductMap, ClaimHeader claimHeader, ClaimTransactionHeader claimtransactionheader, out IEnumerable<ClaimFinancialAmount> reserves, out IEnumerable<ClaimFinancialAmount> payments, out IEnumerable<ClaimFinancialAmount> recoveryReserves)
        {
            var inProgressData = ObjectFactory.Resolve<IInProgressFinancialAmountData>();
            var historicalData = ObjectFactory.Resolve<IHistoricalFinancialAmountData>();

            var claimHeaderArg = new ClaimHeaderArgument(AmountDataSource.Both, claimHeader, claimtransactionheader);

            var validClaimDetails = claimDetailProductMap
                .Where(a => a.Value.ClaimDetailAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.FromClaimHeader)
                .Select(a => a.Key);

            var reserveCalculation = new TotalClaimReserveFinancialCalculation(inProgressData, historicalData);
            var paymentCalcuation = new TotalClaimPaymentFinancialCalculation(inProgressData, historicalData);
            var recoveryReceiptCalculation = new TotalClaimReceiptFinancialCalculation(inProgressData, historicalData);
            var recoveryRerserveCalculation = new TotalClaimRecoveryReserveFinancialCalculation(inProgressData, historicalData);
            bool includeEstimated = productClaimDefinition.IncurredAmountDerivationMethod == (short)StaticValues.IncurredAmountDerivationMethod.PaymentsReservesincludingEstimated;
            bool includeRecoveryEstimated = productClaimDefinition.RecoveryIncurredAmountDerivationMethod == (short)StaticValues.RecoveryIncurredAmountDerivationMethod.ReceiptsRecoveryReservesincludingEstimated;
            reserves = reserveCalculation.ReadLatestClaimReserves(claimHeaderArg, includeEstimated).Where(a => validClaimDetails.Contains(a.ClaimDetailReference));
            payments = paymentCalcuation.ReadClaimPayments(claimHeaderArg).Where(a => validClaimDetails.Contains(a.ClaimDetailReference));
            payments = payments.Concat(recoveryReceiptCalculation.ReadClaimReceipts(claimHeaderArg).Where(a => validClaimDetails.Contains(a.ClaimDetailReference)));
            recoveryReserves = recoveryRerserveCalculation.ReadLatestClaimRecoveryReserves(claimHeaderArg, includeRecoveryEstimated).Where(a => validClaimDetails.Contains(a.ClaimDetailReference));


            var excludedMovementTypes = ClaimsBusinessLogicHelper.GetExcludedMovementTypesFromAutomaticDeductibleCalculations();
            reserves = FilterExcludedMovementTypes(reserves, excludedMovementTypes);
            recoveryReserves = FilterExcludedMovementTypes(recoveryReserves, excludedMovementTypes);
            payments = FilterExcludedMovementTypes(payments, excludedMovementTypes);

            reserves = CreateAmountsForCalculation(reserves);
            recoveryReserves = CreateAmountsForCalculation(recoveryReserves);
            payments = CreateAmountsForCalculation(payments);
        }

        /// <summary>
        /// Load historical reserves/payments/recovery reserves/recovery receipts for the specified claim detail
        /// </summary>
        /// <param name="transactionSource">transaction source</param>
        /// <param name="productClaimDefinition">claim product</param>
        /// <param name="claimDetail">claim detail</param>
        /// <param name="reserves">resulting reserves</param>
        /// <param name="payments">resulting payments and recovery receipts</param>
        /// <param name="recoveryReserves">resulting recovery reserves</param>
        private static void LoadFinancialAmounts(StaticValues.ClaimTransactionSource transactionSource, ProductClaimDefinition productClaimDefinition, ClaimDetail claimDetail, ClaimTransactionHeader claimtransactionheader, out IEnumerable<ClaimFinancialAmount> reserves, out IEnumerable<ClaimFinancialAmount> payments, out IEnumerable<ClaimFinancialAmount> recoveryReserves)
        {
            var inProgressData = ObjectFactory.Resolve<IInProgressFinancialAmountData>();
            var historicalData = ObjectFactory.Resolve<IHistoricalFinancialAmountData>();

            var claimDetailArg = new ClaimDetailArgument(AmountDataSource.Both, claimDetail, null);

            var reserveCalculation = new TotalClaimDetailReserveFinancialCalculation(inProgressData, historicalData);
            var paymentCalcuation = new TotalClaimDetailPaymentFinancialCalculation(inProgressData, historicalData);
            var recoveryReceiptCalculation = new TotalClaimDetailReceiptFinancialCalculation(inProgressData, historicalData);
            var recoveryRerserveCalculation = new TotalClaimDetailRecoveryReserveFinancialCalculation(inProgressData, historicalData);
            bool includeEstimated = productClaimDefinition.RecoveryIncurredAmountDerivationMethod == (short)StaticValues.RecoveryIncurredAmountDerivationMethod.ReceiptsRecoveryReservesincludingEstimated;
            bool includeRecoveryEstimated = productClaimDefinition.RecoveryIncurredAmountDerivationMethod == (short)StaticValues.RecoveryIncurredAmountDerivationMethod.ReceiptsRecoveryReservesincludingEstimated;
            reserves = reserveCalculation.ReadLatestClaimDetailReserves(claimDetailArg, includeEstimated);
            payments = paymentCalcuation.ReadClaimDetailPayments(claimDetailArg);
            payments = payments.Concat(recoveryReceiptCalculation.ReadClaimDetailReceipt(claimDetailArg));
            recoveryReserves = recoveryRerserveCalculation.ReadLatestClaimDetailRecoveryReserves(claimDetailArg, includeRecoveryEstimated);

            var excludedMovementTypes = ClaimsBusinessLogicHelper.GetExcludedMovementTypesFromAutomaticDeductibleCalculations();
            reserves = FilterExcludedMovementTypes(reserves, excludedMovementTypes);
            recoveryReserves = FilterExcludedMovementTypes(recoveryReserves, excludedMovementTypes);
            payments = FilterExcludedMovementTypes(payments, excludedMovementTypes);

            reserves = CreateAmountsForCalculation(reserves);
            recoveryReserves = CreateAmountsForCalculation(recoveryReserves);
            payments = CreateAmountsForCalculation(payments);
        }


        /// <summary>
        /// This method will remove all financial Amounts with moovement Types set as IsExcludedFromAutomaticDeductibleCalculations = true. 
        /// </summary>
        /// <param name="claimFinancialAmounts">The claim Fainancial amounts that needs to be filtered</param>
        /// <param name="excludedMovementTypes">excluded movement types</param>
        /// <returns>The filtered list</returns>
        private static IEnumerable<ClaimFinancialAmount> FilterExcludedMovementTypes(IEnumerable<ClaimFinancialAmount> claimFinancialAmounts, IEnumerable<string> excludedMovementTypes)
        {
            return claimFinancialAmounts
                .Where(amt => !excludedMovementTypes
                    .Any(mvt => amt.MovementType == mvt));
        }


        private static void PopulateRecoveryMovementType(TransactionContext transactionContext, DeductibleDefinition definition, string productCode, IBusinessComponent context)
        {
            string movementType;
            if (definition.FundedMovementType != null && ClaimsBusinessLogicHelper.TryGetRecoveryReserveMovementType(transactionContext, definition.FundedMovementType, productCode, context, out movementType))
            {
                definition.RecoveryFundedMovementType = movementType;
            }

            if (definition.FundedMovementType != null && ClaimsBusinessLogicHelper.TryGetRecoveryReserveMovementType(transactionContext, definition.FundedMovementType, productCode, context, out movementType))
            {
                definition.RecoveryNonFundedMovementType = movementType;
            }
        }

        /// <summary>
        /// Add new claim transaction detail for the new deductible. If payment/resere then stored as +ve if recovery reserve stored as -ve
        /// </summary>
        /// <param name="claimTransactionGroup">claim transaction group to attach to</param>
        /// <param name="amountType">amount type</param>
        /// <param name="deductibleDefinition">deductible definition containing deductible movement type</param>
        /// <param name="amount">deductible amount</param>
        /// <param name="latestNonFundedReserve">latest reserve if any</param>
        /// <returns>Amount s</returns>
        private static decimal AddClaimTransactionDetails(DateTime transactionDate, ClaimTransactionGroup claimTransactionGroup, StaticValues.AmountType amountType, DeductibleDefinition deductibleDefinition, decimal amount, ClaimFinancialAmount latestNonFundedReserve = null)
        {
            decimal convertedAmount;
            if (claimTransactionGroup.ClaimCurrencyCode != claimTransactionGroup.OriginalCurrencyCode)
            {
                convertedAmount = ClaimsBusinessLogicHelper.ConvertCurrencyAmount(claimTransactionGroup.ClaimTransactionHeader, amount, claimTransactionGroup.ClaimCurrencyCode, claimTransactionGroup.OriginalCurrencyCode);
            }
            else
            {
                convertedAmount = amount;
            }


            string fundedMovementType, nonFundedMovementType;
            if (amountType == StaticValues.AmountType.RecoveryReserve)
            {
                convertedAmount = -convertedAmount;
                fundedMovementType = deductibleDefinition.RecoveryFundedMovementType;
                nonFundedMovementType = deductibleDefinition.RecoveryNonFundedMovementType;
            }
            else
            {
                fundedMovementType = deductibleDefinition.FundedMovementType;
                nonFundedMovementType = deductibleDefinition.NonFundedMovementType;
            }



            short? reserveDaySequence = null;
            if (amountType == StaticValues.AmountType.Reserve || amountType == StaticValues.AmountType.RecoveryReserve)
            {
                reserveDaySequence = (short)(latestNonFundedReserve == null ? 10 : latestNonFundedReserve.ReserveDaySequence.GetValueOrDefault(0) + 10);
            }

            var claimTransactionDetail = claimTransactionGroup.AddNewClaimTransactionDetail((short)amountType, nonFundedMovementType);
            PopulateClaimTransactionDetail(claimTransactionDetail, convertedAmount, -Math.Abs(convertedAmount), transactionDate, reserveDaySequence, amountType);
            List<FlattenedTransaction> flattenedTransactions;
            if (GlobalClaimWakeUp.MappedTransactionDetails.TryGetValue(claimTransactionGroup.ClaimTransactionHeader.ClaimHeader.ClaimReference, out flattenedTransactions))
            {
                flattenedTransactions.Add(Mapper.Map<ClaimTransactionDetail, FlattenedTransaction>(claimTransactionDetail));
            }
            if (fundedMovementType != null)
            {
                claimTransactionDetail = claimTransactionGroup.AddNewClaimTransactionDetail((short)amountType, fundedMovementType);
                PopulateClaimTransactionDetail(claimTransactionDetail, -convertedAmount, Math.Abs(convertedAmount), transactionDate, reserveDaySequence, amountType);
                if (GlobalClaimWakeUp.MappedTransactionDetails.TryGetValue(claimTransactionGroup.ClaimTransactionHeader.ClaimHeader.ClaimReference, out flattenedTransactions))
                {
                    flattenedTransactions.Add(Mapper.Map<ClaimTransactionDetail, FlattenedTransaction>(claimTransactionDetail));
                }
            }

            return amount;
        }

        private static void PopulateClaimTransactionDetail(ClaimTransactionDetail claimTransactionDetail, decimal transactionAmount, decimal calcuationSourceAmount, DateTime? reserveDate, short? reserveDaySequence, StaticValues.AmountType amountType)
        {
            if (amountType == StaticValues.AmountType.Reserve || amountType == StaticValues.AmountType.RecoveryReserve)
            {
                ClaimTransactionGroup claimTransGroup = claimTransactionDetail.ClaimTransactionGroup;
                ClaimTransactionHeader claimTransHeader = claimTransGroup.ClaimTransactionHeader;
                var productDefinition = claimTransHeader.GetProductClaimDefinition();
                claimTransactionDetail.ReserveDate = reserveDate;
                transactionAmount = ClaimsBusinessLogicHelper.RoundDecimals(productDefinition.ReserveAmountMaximumNumberOfDecimalPlaces.GetValueOrDefault(0), transactionAmount);
                calcuationSourceAmount = ClaimsBusinessLogicHelper.RoundDecimals(productDefinition.ReserveAmountMaximumNumberOfDecimalPlaces.GetValueOrDefault(0), calcuationSourceAmount);
            }

            claimTransactionDetail.TransactionAmountOriginal = transactionAmount;
            claimTransactionDetail.CalculationSourceAmountOriginal = calcuationSourceAmount;
            ClaimsBusinessLogicHelper.CalculateTransactionAmounts(claimTransactionDetail);
            claimTransactionDetail.ReserveDaySequence = reserveDaySequence;
        }

        private static void ResetAmoundChangedFlag(ClaimTransactionHeader header)
        {
            var claimTransactionDetails = from ctg in header.ClaimTransactionGroups
                                          from ctd in ctg.ClaimTransactionDetails
                                          select ctd;

            foreach (var claimTransactionDetail in claimTransactionDetails)
            {
                claimTransactionDetail.HasSourceAmountOriginalChanged = false;
            }
        }

        private static void PrintFinancialTransaction(ClaimTransactionHeader header)
        {
            var claimTransactionGroup = header.ClaimTransactionGroups.Single();
            _Logger.Debug("Deductibles Calculation Results Begin");
            foreach (var claimTransactionDetail in claimTransactionGroup.ClaimTransactionDetails)
            {
                _Logger.Debug(string.Format("        ClaimTransactionDetail[ MovementType = {0}, Original Amount = {1}, Claim Amount {2}]",
                    claimTransactionDetail.MovementType,
                    claimTransactionDetail.TransactionAmountOriginal,
                    claimTransactionDetail.TransactionAmountClaimCurrency));
            }

            _Logger.Debug("Deductibles Calculation Results End");
        }

        private static bool IsRecovery(StaticValues.ClaimTransactionSource claimTransactionSource)
        {
            return claimTransactionSource == StaticValues.ClaimTransactionSource.RecoveryReceipt || claimTransactionSource == StaticValues.ClaimTransactionSource.RecoveryReserve;
        }

        /// <summary>
        /// Amounts in the database are stored with the opposite signs as are required for calculations. 
        /// Therefore the sign needs to be inverted to make reserves and payments positive and recovery reserves and recovery receipts negative
        /// </summary>
        /// <param name="amounts">the amounts as defined in the database</param>
        /// <returns>new amounts with inverted sign</returns>
        private static IEnumerable<ClaimFinancialAmount> CreateAmountsForCalculation(IEnumerable<ClaimFinancialAmount> amounts)
        {
            var newAmounts = new List<ClaimFinancialAmount>();
            foreach (var amount in amounts)
            {
                var calculationAmount = amount.Copy();
                calculationAmount.TransactionAmountAccounting = -calculationAmount.TransactionAmountAccounting;
                calculationAmount.TransactionAmountClaimCurrency = -calculationAmount.TransactionAmountClaimCurrency;
                calculationAmount.TransactionAmountBase = -calculationAmount.TransactionAmountBase;
                calculationAmount.TransactionAmountOriginal = -calculationAmount.TransactionAmountOriginal;
                newAmounts.Add(calculationAmount);
            }

            return newAmounts;
        }

        /// <summary>
        /// Try and find the latest deductible reserve
        /// </summary>
        /// <param name="reserves">all reserves</param>
        /// <param name="context">context variables</param>
        /// <param name="movementType">movement type of the latest reserve</param>
        /// <returns>latest reserve or null</returns>
        private static ClaimFinancialAmount LatestReserveOrNull(IEnumerable<ClaimFinancialAmount> reserves, FinancialTransactionContext context, string movementType)
        {
            var claimDetailRef = context.ClaimDetail.ClaimDetailReference;
            var origCurrency = context.ClaimTransactionGroup.OriginalCurrencyCode;
            var accCurrency = context.ClaimTransactionGroup.AccountingCurrencyCode;

            return (from reserve in reserves
                    where reserve.ClaimDetailReference == claimDetailRef
                    && reserve.MovementType == movementType
                    && reserve.OriginalCurrencyCode == origCurrency
                    && reserve.AccountingCurrencyCode == accCurrency
                    select reserve)
                    .SingleOrDefault();
        }

        private class DeductibleDefinition
        {
            public bool IsClaimDetailDeductible { get; set; }
            public string NonFundedMovementType { get; set; }
            public string FundedMovementType { get; set; }
            public string RecoveryNonFundedMovementType { get; set; }
            public string RecoveryFundedMovementType { get; set; }
            public decimal Amount { get; set; }

            public IEnumerable<string> GetMovementTypes()
            {
                if (this.NonFundedMovementType != null)
                {
                    yield return this.NonFundedMovementType;
                }

                if (this.FundedMovementType != null)
                {
                    yield return this.FundedMovementType;
                }
            }

            public IEnumerable<string> GetRecoveryMovementTypes()
            {
                if (this.RecoveryNonFundedMovementType != null)
                {
                    yield return this.RecoveryNonFundedMovementType;
                }

                if (this.RecoveryFundedMovementType != null)
                {
                    yield return this.RecoveryFundedMovementType;
                }
            }
        }

        private class FinancialTransactionContext
        {
            public FinancialTransactionContext(ClaimHeader claimHeader, ClaimDetail claimDetail, ClaimTransactionHeader claimTransactionHeader, ClaimTransactionGroup claimTransactionGroup, User user)
            {
                this.ClaimHeader = claimHeader;
                this.ClaimDetail = claimDetail;
                this.ClaimTransactionHeader = claimTransactionHeader;
                this.ClaimTransactionGroup = claimTransactionGroup;
                this.CurrentUser = user;
                this.TransactionSource = (StaticValues.ClaimTransactionSource)this.ClaimTransactionHeader.ClaimTransactionSource;

                this.Product = claimHeader.GetProduct();
                this.ProductClaimDefinition = this.Product.ProductClaimDefinition;
                this.ProductClaimDetails = this.Product.ClaimDetails;
            }

            public StaticValues.ClaimTransactionSource TransactionSource { get; set; }

            public ClaimHeader ClaimHeader { get; private set; }
            public ClaimDetail ClaimDetail { get; private set; }
            public ClaimTransactionHeader ClaimTransactionHeader { get; private set; }
            public ClaimTransactionGroup ClaimTransactionGroup { get; private set; }
            public User CurrentUser { get; private set; }
            public ProductVersion Product { get; set; }
            public ProductClaimDefinition ProductClaimDefinition { get; set; }
            public IEnumerable<ProductClaimDetail> ProductClaimDetails { get; set; }
        }

        private class DeductibleResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
