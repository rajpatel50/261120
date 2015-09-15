using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using AutoMapper;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic.AuthorityCheck;
using Xiap.Framework;
using Xiap.Framework.Data.Claims;
using Xiap.Framework.Extensions;
using Xiap.Framework.Logging;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using XiapClaim = Xiap.Claims.Data.XML;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// A class to handle the XML transform and mapping aspects of Claim Transfer Data. 
    /// This class is effectively a helper to the ClaimTransferDataCollection class.
    /// </summary>
    public class ClaimTransferDataTransform
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Filters the claim details for those attached to a policy and returns the list.
        /// </summary>
        /// <param name="targetClaim">The target claim.</param>
        /// <returns>Returns an array of Claim Details attached at Policy Level</returns>
        public static XiapClaim.ClaimDetail[] FilterClaimDetails(XiapClaim.ClaimHeader targetClaim)
        {
            // Get the Claim Details from the Claim XML
            List<XiapClaim.ClaimDetail> claimDetailList = new List<XiapClaim.ClaimDetail>();

            if (targetClaim.ClaimDetails != null && targetClaim.ClaimDetails.Count() > 0)
            {
                // Cycle through each Claim Detail found
                foreach (XiapClaim.ClaimDetail detail in targetClaim.ClaimDetails)
                {
                    // If the Claim Detail is linked to a Policy, add the to the list.
                    if (detail.PolicyLinkLevel.HasValue || detail.PolicyLinkLevel > 0)
                    {
                        claimDetailList.Add(detail);
                    }
                }
            }

            return claimDetailList.ToArray<XiapClaim.ClaimDetail>();
        }

        /// <summary>
        /// Method used to construct transaction details
        /// </summary>
        /// <param name="targetCTH">Claim Transaction Header</param>
        /// <param name="targetCTG">Claim Transaction Group</param>
        /// <param name="fundedDeductibles">Dictionary collection key: {movement type code}, value: {deductible}</param>
        /// <returns>Claim transaction Detail array</returns>
        public static XiapClaim.ClaimTransactionDetail[] ConstructTransactiondetails(XiapClaim.ClaimTransactionHeader targetCTH, XiapClaim.ClaimTransactionGroup targetCTG, Dictionary<string, string> fundedDeductibles)
        {
            // Get Claim Transaction Details that have a movement type for a Funded Deductible by using the fundedDeductibles dictionary passed in
            IEnumerable<XiapClaim.ClaimTransactionDetail> fundedCTDs = targetCTG.ClaimTransactionDetails.Where(a => fundedDeductibles.ContainsKey(a.MovementType));

            // Get the mirror Claim Transaction Details list, all the claim transaction details that aren't funded deductibles.
            List<XiapClaim.ClaimTransactionDetail> nonFundedCTDs = targetCTG.ClaimTransactionDetails.Where(a => !fundedDeductibles.ContainsKey(a.MovementType)).ToList();

            var historicalData = ObjectFactory.Resolve<HistoricalFinancialAmountData>();

            ClaimDetail claimDetail = new ClaimDetail();
            claimDetail.ClaimDetailID = targetCTG.ClaimDetailID;
            decimal totalPayment = 0;           // Transaction Amount summing
            decimal totalPaymentMovement = 0;   // Movement Amount Summing
            decimal totalReserve = 0;           // Transaction Amount summing
            decimal totalReserveMovement = 0;   // Movement Amount summing

            // If the CTH is from a Payment or a Payment Cancellation, sum non-funded payments
            if (targetCTH.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment || targetCTH.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.PaymentCancellation)
            {
                // Get the payments from the nonFunded CTDs retrieved above.
                IEnumerable<XiapClaim.ClaimTransactionDetail> nonFundedPayments = nonFundedCTDs.Where(a => a.AmountType == (short)StaticValues.AmountType.Payment);
                // Sum all the amounts on the payments
                totalPayment = nonFundedPayments.Sum(a => a.TransactionAmountOriginal.GetValueOrDefault(0));
                totalPaymentMovement = nonFundedPayments.Sum(a => a.MovementAmountOriginal.GetValueOrDefault(0));
            }

            // If CTH is from a payment, a reserve or a Payment Cancellation
            if (targetCTH.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment || targetCTH.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Reserve || targetCTH.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.PaymentCancellation)
            {
                // Find the total reserve amount not attached to funded deductibles for this Claim Transaction Header and any historical CTHs before it.
                totalReserve = CalculateOutstandingReserves(targetCTG, fundedDeductibles, claimDetail, StaticValues.AmountType.Reserve);

                // Sum the total Reserves from just this Claim Transaction Header, over this claim detail
                IEnumerable<XiapClaim.ClaimTransactionDetail> nonFundedReserves = nonFundedCTDs.Where(a => a.AmountType == (short)StaticValues.AmountType.Reserve);
                totalReserveMovement = nonFundedReserves.Sum(a => a.MovementAmountOriginal.GetValueOrDefault(0));

                // If the current reserve is authorised, reduce the total reserve against this claim detail by the amount in this CTH
                if (targetCTH.ReserveAuthorisationStatus != (short)StaticValues.ReserveAuthorisationStatus.ReserveAuthorised)
                {
                    totalReserve = totalReserve - totalReserveMovement;
                }
            }

            // If the Claim Transaction Header is a Recovery Receipt
            if (targetCTH.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt)
            {
                // Get the recovery receipts from the nonFunded CTDs retrieved above.
                IEnumerable<XiapClaim.ClaimTransactionDetail> nonFundedPayments = nonFundedCTDs.Where(a => a.AmountType == (short)StaticValues.AmountType.RecoveryReceipt);
                // Sum all the amounts on the recovery receipts
                totalPayment = nonFundedPayments.Sum(a => a.TransactionAmountOriginal.GetValueOrDefault(0));
                totalPaymentMovement = nonFundedPayments.Sum(a => a.MovementAmountOriginal.GetValueOrDefault(0));
            }

            // If the Claim Transaction Header is a Recovery Receipt or Recovery Reserve
            if (targetCTH.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt || targetCTH.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReserve)
            {
                // Find the total recovery reserve amount not attached to funded deductibles for this Claim Transaction Header and any historical CTHs before it.
                totalReserve = CalculateOutstandingReserves(targetCTG, fundedDeductibles, claimDetail, StaticValues.AmountType.RecoveryReserve);

                // Sum the total Recovery Reserves from just this Claim Transaction Header, over this claim detail
                IEnumerable<XiapClaim.ClaimTransactionDetail> nonFundedReserves = nonFundedCTDs.Where(a => a.AmountType == (short)StaticValues.AmountType.RecoveryReserve);
                totalReserveMovement = nonFundedReserves.Sum(a => a.MovementAmountOriginal.GetValueOrDefault(0));

                // If the current recovery reserve is authorised, reduce the total recovery reserve against this claim detail by the amount in this CTH
                if (targetCTH.RecoveryReserveAuthorisationStatus != (short)StaticValues.RecoveryReserveAuthorisationStatus.RecoveryReserveAuthorised)
                {
                    totalReserve = totalReserve - totalReserveMovement;
                }
            }

            // Returns a list with up to 1 payment type claim transaction detail and up to 1 reserve type claim transaction detail
            // with the totals and movement type of the first CTD of that type in the nonFunded list
            nonFundedCTDs = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(nonFundedCTDs, targetCTH.ClaimTransactionSource.Value, totalPayment, totalReserve, totalPaymentMovement, totalReserveMovement);

            // Add all the funded CTDs retrieved at the start of this method (unaltered)
            nonFundedCTDs.AddRange(fundedCTDs);

            return nonFundedCTDs.ToArray();
        }

        /// <summary>
        /// Use a Stored Procedure Call to collect all the reserves or recovery reserves, 
        /// then total up all in the Original Currency code of the target ClaimTransactionGroup,
        /// which aren't attached to funded deductible movement types.
        /// </summary>
        /// <param name="targetCTG">Claim Transaction Group</param>
        /// <param name="fundedDeductibles">Dictionary data</param>
        /// <param name="claimDetail">claim detail</param>
        /// <param name="amountType">amount type that we are calculating over (e.g. Recovery or RecoveryReserve)</param>
        /// <returns>decimal value</returns>
        private static decimal CalculateOutstandingReserves(XiapClaim.ClaimTransactionGroup targetCTG, Dictionary<string, string> fundedDeductibles, ClaimDetail claimDetail, StaticValues.AmountType amountType)
        {
            IAXAClaimsQuery query = new AXAClaimsQueries();
            // This query is going to retrieve the total of all amounts of the given AmountType against this claim detail
            // for the given Claim Transaction Header and any historical claim transaction headers
            IEnumerable<ClaimFinancialAmount> recoveryReserves = query.ExecuteAccumulatedClaimAmounts(claimDetail.ClaimDetailID, targetCTG.ClaimTransactionHeaderID, amountType);
            decimal total = GetReserveTotal(recoveryReserves, fundedDeductibles, targetCTG.OriginalCurrencyCode);
            return total;
        }

        /// <summary>
        /// Getting the value of total Movement Amount (original currency) of reserves that aren't funded deductible reserves.
        /// </summary>
        /// <param name="reserves">IEnumerable collection</param>
        /// <param name="fundedDeductibles">Dictionary collection</param>
        /// <param name="currency">string value</param>
        /// <returns>decimal value</returns>
        public static decimal GetReserveTotal(IEnumerable<ClaimFinancialAmount> reserves, Dictionary<string, string> fundedDeductibles, string currency)
        {
            decimal total = 0;

            // Find reserves that aren't for funded deductibles, which are in the same Original currency as passed in and total them.
            foreach (ClaimFinancialAmount cfa in reserves)
            {
                if (!fundedDeductibles.ContainsKey(cfa.MovementType) && cfa.OriginalCurrencyCode == currency)
                {
                    total += cfa.MovementAmountOriginal.GetValueOrDefault();
                }
            }

            return total;
        }

        /// <summary>
        /// method used to get first non-funded transaction detail
        /// </summary>
        /// <param name="nonFundedCTDs">IEnumerable collection</param>
        /// <param name="transactionSource">short value</param>
        /// <param name="totalPayment">decimal value</param>
        /// <param name="totalReserve">decimal value</param>
        /// <param name="totalPaymentMovement">decimal value</param>
        /// <param name="totalReserveMovement">decimal value</param>
        /// <returns>List of Claim Transaction detail</returns>
        public static List<XiapClaim.ClaimTransactionDetail> GetFirstNonFundedTransactionDetail(IEnumerable<XiapClaim.ClaimTransactionDetail> nonFundedCTDs, short transactionSource, decimal totalPayment, decimal totalReserve, decimal totalPaymentMovement, decimal totalReserveMovement)
        {
            // Returned list of CTDs
            List<XiapClaim.ClaimTransactionDetail> details = new List<XiapClaim.ClaimTransactionDetail>();
            string movementType = null;

            XiapClaim.ClaimTransactionDetail paymentDetail = null;
            // If we're dealing with a Payment or Payment Cancellation, take the first Claim Trans Detail from the nonFundedCTDs list with a Payment amount type.
            // Otherwise, if this is a recovery receipt, take the first CTD from the list that's a RecoveryReceipt.
            if (transactionSource == (short)StaticValues.ClaimTransactionSource.Payment || transactionSource == (short)StaticValues.ClaimTransactionSource.PaymentCancellation)
            {
                paymentDetail = nonFundedCTDs.Where(a => a.AmountType == (short)StaticValues.AmountType.Payment).FirstOrDefault();
            }
            else if (transactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt)
            {
                paymentDetail = nonFundedCTDs.Where(a => a.AmountType == (short)StaticValues.AmountType.RecoveryReceipt).FirstOrDefault();
            }

            // if transaction source is Payment, cancel payment or Recovery Receipt and total payment is zero 
            // then do not add CTD paymentDetail to returned list as refund cannot be zero
            if (paymentDetail != null && totalPayment != 0)
            {
                paymentDetail.CalculationSourceAmountOriginal = paymentDetail.TransactionAmountOriginal = totalPayment;
                paymentDetail.MovementAmountOriginal = totalPaymentMovement;
                details.Add(paymentDetail);
                movementType = paymentDetail.MovementType;
            }

            XiapClaim.ClaimTransactionDetail reserveDetail = null;
            // If we're dealing with a Payment, Reserve or Payment Cancellation, take the first Claim Trans Detail from the nonFundedCTDs list 
            // with a Reserve amount type.
            // Otherwise, if this is a recovery receipt or recovery reserve, take the first CTD from the list that's a RecoveryReserve.
            if (transactionSource == (short)StaticValues.ClaimTransactionSource.Payment || transactionSource == (short)StaticValues.ClaimTransactionSource.Reserve || transactionSource == (short)StaticValues.ClaimTransactionSource.PaymentCancellation)
            {
                reserveDetail = GetReserveCTD(nonFundedCTDs, (short)StaticValues.AmountType.Reserve, movementType);
            }
            else if (transactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt || transactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReserve)
            {
                reserveDetail = GetReserveCTD(nonFundedCTDs, (short)StaticValues.AmountType.RecoveryReserve, movementType);
            }

            // if this is a reserve or recovery reserve and total reserve amount is zero then do not create reserve transaction detail as refund cannot be zero
            if (reserveDetail != null && totalReserveMovement != 0)
            {
                reserveDetail.CalculationSourceAmountOriginal = reserveDetail.TransactionAmountOriginal = totalReserve;
                reserveDetail.MovementAmountOriginal = totalReserveMovement;
                details.Add(reserveDetail);
            }

            return details;
        }

        /// <summary>
        /// method used to get the resere transaction details
        /// </summary>
        /// <param name="claimTransactiondetails">IEnumerable collection</param>
        /// <param name="amountType">short value</param>
        /// <param name="movementType">string value</param>
        /// <returns>claim transaction detail</returns>
        public static XiapClaim.ClaimTransactionDetail GetReserveCTD(IEnumerable<XiapClaim.ClaimTransactionDetail> claimTransactiondetails, short amountType, string movementType)
        {
            // Select the first CTD for the given Amount and Movement types.
            XiapClaim.ClaimTransactionDetail reserveDetail = claimTransactiondetails.Where(a => a.AmountType == amountType && a.MovementType == movementType).FirstOrDefault();
            if (reserveDetail == null)
            {
                // If we didn't get a Claim Transaction Detail, select on Amount Type only.
                reserveDetail = claimTransactiondetails.Where(a => a.AmountType == amountType).FirstOrDefault();
            }

            return reserveDetail;
        }

        /// <summary>
        /// Method used to extract deductible transactions.
        /// </summary>
        /// <param name="cth">Claim Transaction Header</param>
        /// <param name="fundedDeductibles">Dictionary Collection</param>
        /// <param name="fundedDeductiblePolicies">Dictionary Collection</param>
        /// <param name="riskProductCode">string value</param>
        /// <param name="claimHeaderID">long value</param>
        /// <returns>xml node</returns>
        public static XmlNode ExtractDeductibleTransactions(XiapClaim.ClaimTransactionHeader cth, Dictionary<string, string> fundedDeductibles, Dictionary<string, string> fundedDeductiblePolicies, string riskProductCode, long claimHeaderID)
        {
            _Logger.Info("ClaimTransferDataTransform.ExtractDeductibleTransactions - Start");

            XmlNode rootNode = null;
            XmlDocument doc = null;

            // Create a new object of Claim Trans Group and Claim Trans Detail, ordered by movement type and Claim Transaction Group ID
            // from the current Claim Transaction Header, or a new (empty) object if the CTH is null.
            var movements = from ctg in (cth != null ? cth.ClaimTransactionGroups : new XiapClaim.ClaimTransactionGroup[0])
                            from ctd in ctg.ClaimTransactionDetails
                            orderby ctd.MovementType, ctg.ClaimTransactionGroupID
                            select new { CTG = ctg, CTD = ctd };

            // Create a lookup linked list from fundedDeductibles, only including deductibles as the key for 'movements' entries
            // that match the deductible movement type
            var deductibles = (from fundedDeductible in fundedDeductibles
                               join movement in movements on fundedDeductible.Key equals movement.CTD.MovementType into temp
                               from movement in temp.DefaultIfEmpty()
                               select new { Key = fundedDeductible.Value, Value = movement })
                               .ToLookup(a => a.Key, a => a.Value);

            foreach (var deductible in deductibles)
            {
                XiapClaim.ClaimTransactionGroup newGroup = null;
                var ctgs = new List<XiapClaim.ClaimTransactionGroup>();
                bool areThereOnlyReservesOnAnyClmTrnDetails = false;

                // Check we have a value for this deductible.
                if (!deductible.Contains(null))
                {
                    // Create a new lookup over deductible, keyed on the Claim Transaction Group
                    var ctgGrouping = deductible.ToLookup(a => a.CTG);

                    foreach (var ctgGroup in ctgGrouping)
                    {
                        bool isNew = true;
                        List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

                        // Cycle through the Claim Trans Detail group in the Claim Transaction Group group
                        foreach (var ctdGroup in ctgGroup)
                        {
                            // Create a new Claim Trans Group on the first iteration that will be used to store the Claim Trans Details
                            if (isNew)
                            {
                                XiapClaim.ClaimTransactionGroup ctg = new XiapClaim.ClaimTransactionGroup();
                                ctg = ctdGroup.CTG;

                                IMappingStrategy<XiapClaim.ClaimTransactionGroup, XiapClaim.ClaimTransactionGroup> strategy = new XiapClaimTransactionGroupMapper();
                                newGroup = strategy.MapSourceToTarget(ctg);
                                isNew = false;
                            }

                            ctdList.Add(ctdGroup.CTD);
                        }

                        // Add all our CTDs to the group we created on the first cycle
                        newGroup.ClaimTransactionDetails = ctdList.ToArray();

                        // Check if we only have reserves on these claim transaction details
                        // assuming we haven't already found a CTG with only reserves on.
                        // NOTE: as it stands we only expect one CTG for AXA
                        if (!areThereOnlyReservesOnAnyClmTrnDetails)
                        {
                            areThereOnlyReservesOnAnyClmTrnDetails = AreThereOnlyReservesOnClaimTransactionDetails(ctdList);
                        }

                        // Add the CTG to the list of CTGs
                        ctgs.Add(newGroup);
                    }
                }

                // Create a new XML Document if necessary
                if (doc == null)
                {
                    doc = new XmlDocument();
                    // Load in a new Deductibles node.
                    doc.LoadXml(ClaimConstants.DED_DEDUCTIBLES_TAG);
                }

                // if rootNode hasn't been initialised, set it to the XML documents DocumentElement
                if (rootNode == null)
                {
                    rootNode = doc.DocumentElement;
                }

                XmlNode node = null;
                AddPolicyAndProductAttribute(rootNode, doc, fundedDeductiblePolicies, deductible.Key, riskProductCode, ref node);
                XmlDocument newDoc = new XmlDocument();

                if (ctgs.Count > 0)
                {
                    // If we got any Claim Transaction Groups from the above processing, convert to an array from a list
                    // then serialise
                    XiapClaim.ClaimTransactionGroups deductibleGroups = new XiapClaim.ClaimTransactionGroups { ClaimTransactionGroup = ctgs.ToArray() };
                    string deductibleXML = GetSerializedXml(deductibleGroups);
                    // Create a new XML Document node and add to the deductible node previously created, with teh Policy and Product attributes.
                    newDoc.LoadXml(deductibleXML);
                    XmlNode newNode = doc.ImportNode(newDoc.DocumentElement, true);
                    node.AppendChild(newNode);
                }

                if (cth != null)
                {
                    // We have a Claim Transaction Header passed in so create a Transaction Source attribute on our Deductibles node
                    // and store the Transaction Source to it (this will be a numeric value converted to a string).
                    XmlAttribute transSourceAttribute = doc.CreateAttribute(ClaimConstants.DED_TRANSACTIONSOURCE);

                    transSourceAttribute.InnerText = GetTransactionSourceForDeductibles(areThereOnlyReservesOnAnyClmTrnDetails, ctgs, cth.ClaimTransactionSource);
                    node.Attributes.Append(transSourceAttribute);
                }
            }


            if (cth == null && deductibles.IsNullOrEmpty() && IsFinancialTransactionExistsInClaim(claimHeaderID))
            {
                // We have no Claim Transaction Header and we found no deductibles, but there are still Financial Transactions on teh claim
                // so we need to build up our document, since there will have been nothing written out yet.

                // Create the XML Document with the deductible tags if necessary
                if (doc == null)
                {
                    doc = new XmlDocument();
                    doc.LoadXml(ClaimConstants.DED_DEDUCTIBLES_TAG);
                }

                // Create the root node, if necessary
                if (rootNode == null)
                {
                    rootNode = doc.DocumentElement;
                }

                // Within the deductibles node, create a node for each deductible with the appropriate policy and product attribute on each.
                XmlNode node = null;
                if (fundedDeductiblePolicies.ContainsKey(ClaimConstants.DED_DEDUCTIBLE01))
                {
                    AddPolicyAndProductAttribute(rootNode, doc, fundedDeductiblePolicies, ClaimConstants.DED_DEDUCTIBLE01, riskProductCode, ref node);
                }

                if (fundedDeductiblePolicies.ContainsKey(ClaimConstants.DED_DEDUCTIBLE02))
                {
                    AddPolicyAndProductAttribute(rootNode, doc, fundedDeductiblePolicies, ClaimConstants.DED_DEDUCTIBLE02, riskProductCode, ref node);
                }

                if (fundedDeductiblePolicies.ContainsKey(ClaimConstants.DED_DEDUCTIBLE03))
                {
                    AddPolicyAndProductAttribute(rootNode, doc, fundedDeductiblePolicies, ClaimConstants.DED_DEDUCTIBLE03, riskProductCode, ref node);
                }

                if (fundedDeductiblePolicies.ContainsKey(ClaimConstants.DED_DEDUCTIBLE04))
                {
                    AddPolicyAndProductAttribute(rootNode, doc, fundedDeductiblePolicies, ClaimConstants.DED_DEDUCTIBLE04, riskProductCode, ref node);
                }

                if (fundedDeductiblePolicies.ContainsKey(ClaimConstants.DED_DEDUCTIBLE05))
                {
                    AddPolicyAndProductAttribute(rootNode, doc, fundedDeductiblePolicies, ClaimConstants.DED_DEDUCTIBLE05, riskProductCode, ref node);
                }
            }

            if (_Logger.IsDebugEnabled && rootNode != null && rootNode.InnerXml != null)
            {
                _Logger.Debug("ClaimTransferDataTransform.ExtractDeductibleTransactions - Dump deductibles");
                _Logger.Debug(rootNode.InnerXml);
            }

            _Logger.Info("ClaimTransferDataTransform.ExtractDeductibleTransactions - Finish");

            return rootNode;
        }

        /// <summary>
        /// Ares the there only reserves on claim transaction details. This method first counts to see how many non-reserve
        /// items there are. If there are none, we check if there are any reserve items. If there are, we can return true.
        /// </summary>
        /// <param name="ctds">The claim transaction details to search through</param>
        /// <returns>True if there are only reserves, false otherwise.</returns>
        public static bool AreThereOnlyReservesOnClaimTransactionDetails(List<XiapClaim.ClaimTransactionDetail> ctds)
        {
            _Logger.Info("ClaimTransferDataTransform.AreThereOnlyReservesOnClaimTransactionDetails");

            var numberOfPaymentAmountTypes = (from ctd in ctds
                                                   where ctd.AmountType != (short)StaticValues.AmountType.Reserve
                                                   select ctd.AmountType).Count();

            if (numberOfPaymentAmountTypes == 0)
            {
                var hasReservesWithNonZeroMovements = (from ctd in ctds
                                                       where ctd.AmountType == (short)StaticValues.AmountType.Reserve && ctd.MovementAmountOriginal != 0
                                                       select ctd.AmountType).Any();
                if (hasReservesWithNonZeroMovements)
                {
                    return true;
                }
            }

            _Logger.Info("ClaimTransferDataTransform.AreThereOnlyReservesOnClaimTransactionDetails RETURNS FALSE");

            return false;
        }

        /// <summary>
        /// Gets the transaction source for deductibles as a string. If there are only reserves
        /// on a transaction that is payment cancellation, we change the source to reserve.
        /// NOTE: if we decide we want to apply this logic even if it's a Payment or Recovery Receipt
        /// then the initial 'if' statement should only check the areThereOnlyReservesOnAnyClmTrnDetails boolean
        /// and the block for Recoveries should be uncommented.
        /// </summary>
        /// <param name="areThereOnlyReservesOnAnyClmTrnDetails">if set to <c>true</c> there are only reserves on any CLM TRN details</param>
        /// <param name="ctgs">The list of Claim Transaction Groups [Not in use until we decide we need to process recoveries]</param>
        /// <param name="ClaimTransactionSource">The claim transaction source from the Claim Transaction Header</param>
        /// <returns>String value of the Claim Transaction Source</returns>
        private static string GetTransactionSourceForDeductibles(bool areThereOnlyReservesOnAnyClmTrnDetails, List<XiapClaim.ClaimTransactionGroup> ctgs, short? ClaimTransactionSource)
        {
            _Logger.Info("ClaimTransferDataTransform.GetTransactionSourceForDeductibles");


            // If we have only reserves on a cancellation then we should change the type on the CTH to be a reserve.
            if (areThereOnlyReservesOnAnyClmTrnDetails && ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.PaymentCancellation)
            {
                return Convert.ToString((short)StaticValues.ClaimTransactionSource.Reserve);
            }

            /*
             * The following code will allow us to process this for Recoveries.
            // If the ClaimTransactionHeader is a RecoveryReceipt but now only conatans Recovery Reserves, set to a Recovery Reserve type.
            if (ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt)
            {
                var hasReceipts = (from ctg in ctgs
                                   where ctg.ClaimTransactionDetails != null
                                   from ctd in ctg.ClaimTransactionDetails
                                   where ctd.AmountType == (short)StaticValues.AmountType.RecoveryReceipt
                                   select ctd.AmountType)
                             .Any();

                if (!hasReceipts)
                {
                    return Convert.ToString((short)StaticValues.ClaimTransactionSource.RecoveryReserve);
                }
            }
            */

            return Convert.ToString(ClaimTransactionSource);
        }

        /// <summary>
        /// Call the Claims Queries to check whether any financial transaction exists on claim or not.
        /// </summary>
        /// <param name="claimHeaderID">claim header id</param>
        /// <returns>bool value</returns>
        private static bool IsFinancialTransactionExistsInClaim(long claimHeaderID)
        {
            IAXAClaimsQuery claimQuery = ObjectFactory.Resolve<IAXAClaimsQuery>();
            return claimQuery.HasFinancialTransactionInTheClaim(claimHeaderID);
        }

        /// <summary>
        /// Used to get captive or service product code, as applicable.
        /// </summary>
        /// <param name="riskProductCode">string value</param>
        /// <param name="policyRef">string value</param>
        /// <returns>string value</returns>
        public static string GetCaptiveProductCode(string riskProductCode, string policyRef)
        {
            string captiveProductCode = null;

            // Only process if we've been provided with values for Product Code and Policy Reference
            if (riskProductCode != null && policyRef != null)
            {
                // Policy references ending 'SE' or beginning 'XUS' are Service policies
                if (policyRef.Length > 11 && (policyRef.Substring(10, 2) == "SE" || policyRef.Substring(0, 3) == "XUS"))
                {
                    // Get the appropriate service product name for Liability or Motor products, respectively.
                    if (riskProductCode == ClaimConstants.DED_PRODUCT_LIABCLAIM || riskProductCode == ClaimConstants.DED_PRODUCT_GBIPC)
                    {
                        captiveProductCode = ClaimConstants.DED_PRODUCT_GBSPC;
                    }
                    else if (riskProductCode == ClaimConstants.DED_PRODUCT_MOTOR || riskProductCode == ClaimConstants.DED_PRODUCT_GBIMO)
                    {
                        captiveProductCode = ClaimConstants.DED_PRODUCT_GBSMO;
                    }
                }
                else
                {
                    // This is a Captive policy so we need to return the normal Liability or Motor product.
                    if (riskProductCode == ClaimConstants.DED_PRODUCT_LIABCLAIM || riskProductCode == ClaimConstants.DED_PRODUCT_GBIPC)
                    {
                        captiveProductCode = ClaimConstants.DED_PRODUCT_GBIPC;
                    }
                    else if (riskProductCode == ClaimConstants.DED_PRODUCT_MOTOR || riskProductCode == ClaimConstants.DED_PRODUCT_GBIMO)
                    {
                        captiveProductCode = ClaimConstants.DED_PRODUCT_GBIMO;
                    }
                }
            }

            return captiveProductCode;
        }

        /// <summary>
        /// Check whether the funded is deductible type.
        /// </summary>
        /// <param name="deductible">string value</param>
        /// <param name="fundedDeductiblePolicies">dictionary collection</param>
        /// <param name="attachedPolicyRef">string value</param>
        /// <returns>bool value</returns>
        public static bool IsFundedDeductibleType(string deductible, Dictionary<string, string> fundedDeductiblePolicies, string attachedPolicyRef)
        {
            bool retVal = false;

            // fundedDeductiblePolicies is a Dictionary of format Key = {deductible}, Value = {deductible policy reference}
            // Check if we have a policy for the given Deductible
            if (fundedDeductiblePolicies.ContainsKey(deductible))
            {
                string policyRef = fundedDeductiblePolicies[deductible];
                if (policyRef != null && policyRef.Length > 0 && policyRef != attachedPolicyRef)
                {
                    // We have a funded deductible type.
                    retVal = true;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Gets the Product Code for the given UW ProductVersion
        /// </summary>
        /// <param name="entities">metadata entity value</param>
        /// <param name="productVersionID">nullable long vlaue</param>
        /// <returns>string value</returns>
        public static string GetRiskProductCode(MetadataEntities entities, long? productVersionID)
        {
            string productCode = null;

            productCode = (from product in entities.ProductVersion
                           where product.ProductVersionID == productVersionID
                           select product.Product.Code).SingleOrDefault();

            return productCode;
        }

        /// <summary>
        /// method used to get serialized xml of the Deductible Groups.
        /// </summary>
        /// <param name="deductible01Groups">Claim Transaction Group</param>
        /// <returns>string value</returns>
        private static string GetSerializedXml(XiapClaim.ClaimTransactionGroups deductible01Groups)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XiapClaim.ClaimTransactionGroups));
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, deductible01Groups);
            return sw.ToString();
        }

        /// <summary>
        /// Creates a new node for the Deductible and adds the policy and product attribute to it.
        /// </summary>
        /// <param name="rootNode">xml node</param>
        /// <param name="doc">Xml Document</param>
        /// <param name="fundedDeductiblePolicies">Dictionary collection</param>
        /// <param name="ded_deductible">string value</param>
        /// <param name="riskProductCode">string value</param>
        /// <param name="node">xml node - passed by reference</param>
        private static void AddPolicyAndProductAttribute(XmlNode rootNode, XmlDocument doc, Dictionary<string, string> fundedDeductiblePolicies, string ded_deductible, string riskProductCode, ref XmlNode node)
        {
            // Add the string ded_deductible to the Document. This returns the node that has been added.
            node = rootNode.AppendChild(doc.CreateElement(ded_deductible));
            // Add an attribute for PolicyReference then fill it with the appropriate policy reference from the fundedDeductiblePolicies list
            XmlAttribute polRefAttribute = doc.CreateAttribute(ClaimConstants.DED_POLICYREFERENCE);
            polRefAttribute.InnerText = fundedDeductiblePolicies[ded_deductible];
            // Add this attribute to the new deductible node.
            node.Attributes.Append(polRefAttribute);

            // Get the Product Code for the Deductible policy
            string captiveProductCode = GetCaptiveProductCode(riskProductCode, fundedDeductiblePolicies[ded_deductible]);
            if (string.IsNullOrWhiteSpace(captiveProductCode) == false)
            {
                // If we have a product code, add it as a new Attribute, like the Policy Reference
                XmlAttribute productAttribute = doc.CreateAttribute(ClaimConstants.DED_PRODUCT);
                productAttribute.InnerText = captiveProductCode;
                node.Attributes.Append(productAttribute);
            }

            // node is passed by reference and so is updated
        }

        /// <summary>
        /// check if any reserve with non zero movement exists.
        /// </summary>
        /// <param name="ctds">List collection</param>
        /// <returns>bool value</returns>
        public static bool IsReserveWithNonZeroMovementsPresent(List<XiapClaim.ClaimTransactionDetail> ctds)
        {
            _Logger.Info("ClaimTransferDataTransform.IsReserveWithNonZeroMovementsPresent");

            // if the payment with a zero transaction amount sum has associated reserves with a non-zero movement amount sum,
            // return true.
            var hasPaymentsWithTransactionsZero = (from ctd in ctds
                                                   where ctd.AmountType == (short)StaticValues.AmountType.Payment && ctd.TransactionAmountOriginal == 0
                                                   select ctd.AmountType).Any();

            if (hasPaymentsWithTransactionsZero)
            {
                var hasReservesWithNonZeroMovements = (from ctd in ctds
                                                       where ctd.AmountType == (short)StaticValues.AmountType.Reserve && ctd.MovementAmountOriginal != 0
                                                       select ctd.AmountType).Any();
                if (hasReservesWithNonZeroMovements)
                {
                    return true;
                }
            }

            _Logger.Info("ClaimTransferDataTransform.IsReserveWithNonZeroMovementsPresent RETURNS FALSE");

            return false;
        }

        /// <summary>
        /// class of XiapClaimTransactionGroupMapper
        /// </summary>
        public class XiapClaimTransactionGroupMapper : IMappingStrategy<XiapClaim.ClaimTransactionGroup, XiapClaim.ClaimTransactionGroup>
        {
            private static object syncObject = new object();
            private static volatile bool IsConfigured;

            private void ConfigureMapping()
            {
                if (!IsConfigured)
                {
                    lock (syncObject)
                    {
                        if (!IsConfigured)
                        {
                            Mapper.CreateMap<XiapClaim.ClaimTransactionGroup, XiapClaim.ClaimTransactionGroup>();
                            Mapper.AssertConfigurationIsValid();
                            IsConfigured = true;
                        }
                    }
                }
            }

            /// <summary>
            /// Map the source to target.
            /// </summary>
            /// <param name="claimTransactionGroup">Claim Transaction Group</param>
            /// <returns>Claim Transaction Group</returns>
            public XiapClaim.ClaimTransactionGroup MapSourceToTarget(XiapClaim.ClaimTransactionGroup claimTransactionGroup)
            {
                this.ConfigureMapping();

                return Mapper.Map<XiapClaim.ClaimTransactionGroup, XiapClaim.ClaimTransactionGroup>(claimTransactionGroup);
            }
        }
    }
}

