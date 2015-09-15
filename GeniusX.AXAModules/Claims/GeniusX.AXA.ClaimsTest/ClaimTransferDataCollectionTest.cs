using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GeniusX.AXA.Claims.BusinessLogic;
using Xiap.Metadata.Data.Enums;
using Xiap.Claims.BusinessLogic.AuthorityCheck;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.Data.GenericDataSets;
using Xiap.Framework.Data.Claims;
using XiapClaim = Xiap.Claims.Data.XML;

namespace GeniusX.AXA.ClaimsTest
{
	//// <summary>
	//// Summary description for ClaimTransferDataCollectionTest
	//// </summary>
	[TestClass]
	public class ClaimTransferDataCollectionTest
	{
       private const string liabClaimProduct = "CGBIPC";
		// Please do not remove - useful for testing against a DB
        ////[TestMethod]
        ////public void BJDTest()
        ////{
        ////    var parameters = new Dictionary<string, object>();
        ////    parameters["reference"] = "XUK0002344LI";
        ////    parameters["detailReference"] = "2997";
        ////    parameters["dateTime"] = new DateTime(2012, 1, 1);

        ////    ClaimTransferDataCollection dataCollection = new ClaimTransferDataCollection();
        ////    XmlElement output = dataCollection.GetData(parameters);
        ////}

		[TestMethod]
		public void FilterClaimDetails_NoValidClaimDetails_NoneReturned()
		{
			List<XiapClaim.ClaimDetail> xiapClaimDetails = new List<XiapClaim.ClaimDetail>();

			XiapClaim.ClaimDetail cd1 = new XiapClaim.ClaimDetail();
			cd1.ClaimDetailID = 1;
			cd1.PolicyLinkLevel = null;
			xiapClaimDetails.Add(cd1);

			XiapClaim.ClaimHeader ch = new XiapClaim.ClaimHeader();
			ch.ClaimDetails = xiapClaimDetails.ToArray();

			XiapClaim.ClaimDetail[] results = ClaimTransferDataTransform.FilterClaimDetails(ch);

			Assert.IsNotNull(results);
			Assert.AreEqual(0, results.Count());
		}

		[TestMethod]
		public void FilterClaimDetails_OneValidClaimDetail_OneReturned()
		{
			List<XiapClaim.ClaimDetail> xiapClaimDetails = new List<XiapClaim.ClaimDetail>();

			XiapClaim.ClaimDetail cd1 = new XiapClaim.ClaimDetail();
			cd1.ClaimDetailID = 1;
			cd1.PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Header;
			xiapClaimDetails.Add(cd1);

			XiapClaim.ClaimHeader ch = new XiapClaim.ClaimHeader();
			ch.ClaimDetails = xiapClaimDetails.ToArray();

			XiapClaim.ClaimDetail[] results = ClaimTransferDataTransform.FilterClaimDetails(ch);

			Assert.IsNotNull(results);
			Assert.AreEqual(1, results.Count());
			Assert.IsTrue(results.Contains(cd1));
		}

		[TestMethod]
		public void FilterClaimDetails_TwoValidandTwoInvalidClaimDetails_TwoReturned()
		{
			List<XiapClaim.ClaimDetail> xiapClaimDetails = new List<XiapClaim.ClaimDetail>();

			XiapClaim.ClaimDetail cd1 = new XiapClaim.ClaimDetail();
			cd1.ClaimDetailID = 1;
			cd1.PolicyLinkLevel = null;
			xiapClaimDetails.Add(cd1);

			XiapClaim.ClaimDetail cd2 = new XiapClaim.ClaimDetail();
			cd2.ClaimDetailID = 2;
			cd2.PolicyLinkLevel = 0;
			xiapClaimDetails.Add(cd2);

			XiapClaim.ClaimDetail cd3 = new XiapClaim.ClaimDetail();
			cd3.ClaimDetailID = 3;
			cd3.PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Header;
			xiapClaimDetails.Add(cd3);

			XiapClaim.ClaimDetail cd4 = new XiapClaim.ClaimDetail();
			cd4.ClaimDetailID = 4;
			cd4.PolicyLinkLevel = null;
			xiapClaimDetails.Add(cd4);

			XiapClaim.ClaimHeader ch = new XiapClaim.ClaimHeader();
			ch.ClaimDetails = xiapClaimDetails.ToArray();

			XiapClaim.ClaimDetail[] results = ClaimTransferDataTransform.FilterClaimDetails(ch);

			Assert.IsNotNull(results);
			Assert.AreEqual(2, results.Count());
			Assert.IsFalse(results.Contains(cd1));
			Assert.IsTrue(results.Contains(cd2));
			Assert.IsTrue(results.Contains(cd3));
			Assert.IsFalse(results.Contains(cd4));
		}
		
		[TestMethod]
		public void GetReserveTotal_OneNonDeductibles_AmountSetTo1()
		{
			Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
			fundedDeductibles.Add("A", "Deductible01");

			ClaimFinancialAmount cfa1 = new ClaimFinancialAmount();
			cfa1.MovementType = "1";
			cfa1.MovementAmountOriginal = 1;
			cfa1.OriginalCurrencyCode = "GBP";

			List<ClaimFinancialAmount> cfaList = new List<ClaimFinancialAmount>();
			cfaList.Add(cfa1);

			decimal result = ClaimTransferDataTransform.GetReserveTotal(cfaList, fundedDeductibles, "GBP");

			Assert.AreEqual(1, result);
		}

		[TestMethod]
		public void GetReserveTotal_MixedDeductibleandNonDeductibles_AmountSetTo3()
		{
			Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
			fundedDeductibles.Add("A", "Deductible01");
			fundedDeductibles.Add("B", "Deductible02");

			ClaimFinancialAmount cfa1 = new ClaimFinancialAmount();
			cfa1.MovementType = "1";
			cfa1.MovementAmountOriginal = 1;
			cfa1.OriginalCurrencyCode = "GBP";

			ClaimFinancialAmount cfa2 = new ClaimFinancialAmount();
			cfa2.MovementType = "A";
			cfa2.MovementAmountOriginal = 10;
			cfa2.OriginalCurrencyCode = "GBP";

			ClaimFinancialAmount cfa3 = new ClaimFinancialAmount();
			cfa3.MovementType = "2";
			cfa3.MovementAmountOriginal = 2;
			cfa3.OriginalCurrencyCode = "GBP";

			ClaimFinancialAmount cfa4 = new ClaimFinancialAmount();
			cfa4.MovementType = "B";
			cfa4.MovementAmountOriginal = 20;
			cfa4.OriginalCurrencyCode = "GBP";

			List<ClaimFinancialAmount> cfaList = new List<ClaimFinancialAmount>();
			cfaList.Add(cfa1);
			cfaList.Add(cfa2);
			cfaList.Add(cfa3);
			cfaList.Add(cfa4);

			decimal result = ClaimTransferDataTransform.GetReserveTotal(cfaList, fundedDeductibles, "GBP");

			Assert.AreEqual(3, result);
		}

		[TestMethod]
		public void GetFirstNonFundedTransactionDetail_NoTransactions_NoCTDReturned()
		{
			List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

			List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.Payment, 10, 20, 30, 40);

			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void GetFirstNonFundedTransactionDetail_PaymentOnly_FirstPaymentCTDReturned()
		{
			List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.AmountType = (short)StaticValues.AmountType.Payment;
			ctd1.MovementType = "1";

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.AmountType = (short)StaticValues.AmountType.RecoveryReceipt;
			ctd2.MovementType = "A";

			ctdList.Add(ctd1);
			ctdList.Add(ctd2);

			List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.Payment, 10, 20, 30, 40);

			Assert.AreEqual(1, results.Count);
			XiapClaim.ClaimTransactionDetail ctdResult = results.First();
			Assert.AreEqual(10, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(10, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(30, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("1", ctdResult.MovementType);
		}

		[TestMethod]
		public void GetFirstNonFundedTransactionDetail_ReceiptOnly_FirstReceiptCTDReturned()
		{
			List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.AmountType = (short)StaticValues.AmountType.Payment;
			ctd1.MovementType = "1";

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.AmountType = (short)StaticValues.AmountType.RecoveryReceipt;
			ctd2.MovementType = "A";

			ctdList.Add(ctd1);
			ctdList.Add(ctd2);

			List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.RecoveryReceipt, 10, 20, 30, 40);

			Assert.AreEqual(1, results.Count);
			XiapClaim.ClaimTransactionDetail ctdResult = results.First();
			Assert.AreEqual(10, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(10, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(30, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("A", ctdResult.MovementType);
		}

		[TestMethod]
		public void GetFirstNonFundedTransactionDetail_ReserveOnly_FirstReserveCTDReturned()
		{
			List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.AmountType = (short)StaticValues.AmountType.RecoveryReserve;
			ctd1.MovementType = "X";
			
			ctdList.Add(ctd1);

			List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.RecoveryReceipt, 10, 20, 30, 40);

			Assert.AreEqual(1, results.Count);
			XiapClaim.ClaimTransactionDetail ctdResult = results.First();
			Assert.AreEqual(20, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(20, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(40, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("X", ctdResult.MovementType);
		}

		[TestMethod]
		public void GetFirstNonFundedTransactionDetail_MatchedPaymentAndReserve_FirstPaymentAndReserveCTDReturned()
		{
			List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.AmountType = (short)StaticValues.AmountType.Payment;
			ctd1.MovementType = "1";

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.AmountType = (short)StaticValues.AmountType.RecoveryReceipt;
			ctd2.MovementType = "A";

			XiapClaim.ClaimTransactionDetail ctd3 = new XiapClaim.ClaimTransactionDetail();
			ctd3.AmountType = (short)StaticValues.AmountType.Reserve;
			ctd3.MovementType = "X";

			XiapClaim.ClaimTransactionDetail ctd4 = new XiapClaim.ClaimTransactionDetail();
			ctd4.AmountType = (short)StaticValues.AmountType.Reserve;
			ctd4.MovementType = "1";

			ctdList.Add(ctd1);
			ctdList.Add(ctd2);
			ctdList.Add(ctd3);
			ctdList.Add(ctd4);

			List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.Payment, 10, 20, 30, 40);

			Assert.AreEqual(2, results.Count);

			XiapClaim.ClaimTransactionDetail ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.Payment).First();
			Assert.AreEqual(10, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(10, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(30, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("1", ctdResult.MovementType);

			ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.Reserve).First();
			Assert.AreEqual(20, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(20, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(40, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("1", ctdResult.MovementType);
		}

        [TestMethod]
        public void GetFirstNonFundedTransactionDetail_MatchedPaymentAndReserve_FirstCancelledPaymentCTDReturned()
        {
            List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

            XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
            ctd1.AmountType = (short)StaticValues.AmountType.Payment;
            ctd1.MovementType = "1";

            XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
            ctd2.AmountType = (short)StaticValues.AmountType.RecoveryReceipt;
            ctd2.MovementType = "A";

            XiapClaim.ClaimTransactionDetail ctd3 = new XiapClaim.ClaimTransactionDetail();
            ctd3.AmountType = (short)StaticValues.AmountType.Reserve;
            ctd3.MovementType = "X";

            XiapClaim.ClaimTransactionDetail ctd4 = new XiapClaim.ClaimTransactionDetail();
            ctd4.AmountType = (short)StaticValues.AmountType.Reserve;
            ctd4.MovementType = "1";

            ctdList.Add(ctd1);
            ctdList.Add(ctd2);
            ctdList.Add(ctd3);
            ctdList.Add(ctd4);

            List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.PaymentCancellation, 10, 20, 30, 40);

            Assert.AreEqual(2, results.Count);

            XiapClaim.ClaimTransactionDetail ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.Payment).First();
            Assert.AreEqual(10, ctdResult.CalculationSourceAmountOriginal);
            Assert.AreEqual(10, ctdResult.TransactionAmountOriginal);
            Assert.AreEqual(30, ctdResult.MovementAmountOriginal);
            Assert.AreEqual("1", ctdResult.MovementType);

            ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.Reserve).First();
            Assert.AreEqual(20, ctdResult.CalculationSourceAmountOriginal);
            Assert.AreEqual(20, ctdResult.TransactionAmountOriginal);
            Assert.AreEqual(40, ctdResult.MovementAmountOriginal);
            Assert.AreEqual("1", ctdResult.MovementType);    
        }

		[TestMethod]
		public void GetFirstNonFundedTransactionDetail_MisMatchedPaymentAndReserve_FirstPaymentAndReserveCTDReturned()
		{
			List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.AmountType = (short)StaticValues.AmountType.Payment;
			ctd1.MovementType = "1";

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.AmountType = (short)StaticValues.AmountType.RecoveryReceipt;
			ctd2.MovementType = "A";

			XiapClaim.ClaimTransactionDetail ctd3 = new XiapClaim.ClaimTransactionDetail();
			ctd3.AmountType = (short)StaticValues.AmountType.Reserve;
			ctd3.MovementType = "X";

			ctdList.Add(ctd1);
			ctdList.Add(ctd2);
			ctdList.Add(ctd3);

			List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.Payment, 10, 20, 30, 40);

			Assert.AreEqual(2, results.Count);

			XiapClaim.ClaimTransactionDetail ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.Payment).First();
			Assert.AreEqual(10, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(10, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(30, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("1", ctdResult.MovementType);

			ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.Reserve).First();
			Assert.AreEqual(20, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(20, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(40, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("X", ctdResult.MovementType);
		}

		[TestMethod]
		public void GetFirstNonFundedTransactionDetail_MatchedReceiptAndReserve_FirstReceiptAndReserveCTDReturned()
		{
			List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.AmountType = (short)StaticValues.AmountType.Payment;
			ctd1.MovementType = "1";

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.AmountType = (short)StaticValues.AmountType.RecoveryReceipt;
			ctd2.MovementType = "A";

			XiapClaim.ClaimTransactionDetail ctd3 = new XiapClaim.ClaimTransactionDetail();
			ctd3.AmountType = (short)StaticValues.AmountType.Reserve;
			ctd3.MovementType = "X";

			XiapClaim.ClaimTransactionDetail ctd4 = new XiapClaim.ClaimTransactionDetail();
			ctd4.AmountType = (short)StaticValues.AmountType.RecoveryReserve;
			ctd4.MovementType = "A";

			ctdList.Add(ctd1);
			ctdList.Add(ctd2);
			ctdList.Add(ctd3);
			ctdList.Add(ctd4);

			List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.RecoveryReceipt, 10, 20, 30, 40);

			Assert.AreEqual(2, results.Count);

			XiapClaim.ClaimTransactionDetail ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.RecoveryReceipt).First();
			Assert.AreEqual(10, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(10, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(30, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("A", ctdResult.MovementType);

			ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.RecoveryReserve).First();
			Assert.AreEqual(20, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(20, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(40, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("A", ctdResult.MovementType);
		}

		[TestMethod]
		public void GetFirstNonFundedTransactionDetail_MisMatchedReceiptAndReserve_FirstReceiptAndReserveCTDReturned()
		{
			List<XiapClaim.ClaimTransactionDetail> ctdList = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.AmountType = (short)StaticValues.AmountType.Payment;
			ctd1.MovementType = "1";

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.AmountType = (short)StaticValues.AmountType.RecoveryReceipt;
			ctd2.MovementType = "A";

			XiapClaim.ClaimTransactionDetail ctd3 = new XiapClaim.ClaimTransactionDetail();
			ctd3.AmountType = (short)StaticValues.AmountType.RecoveryReserve;
			ctd3.MovementType = "X";

			ctdList.Add(ctd1);
			ctdList.Add(ctd2);
			ctdList.Add(ctd3);

			List<XiapClaim.ClaimTransactionDetail> results = ClaimTransferDataTransform.GetFirstNonFundedTransactionDetail(ctdList, (short)StaticValues.ClaimTransactionSource.RecoveryReceipt, 10, 20, 30, 40);

			Assert.AreEqual(2, results.Count);

			XiapClaim.ClaimTransactionDetail ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.RecoveryReceipt).First();
			Assert.AreEqual(10, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(10, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(30, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("A", ctdResult.MovementType);

			ctdResult = results.Where(a => a.AmountType == (short)StaticValues.AmountType.RecoveryReserve).First();
			Assert.AreEqual(20, ctdResult.CalculationSourceAmountOriginal);
			Assert.AreEqual(20, ctdResult.TransactionAmountOriginal);
			Assert.AreEqual(40, ctdResult.MovementAmountOriginal);
			Assert.AreEqual("X", ctdResult.MovementType);
		}

		[TestMethod]
		public void ExtractDeductibleTransactions_NoDeductibleCTDOneFundedDedutible_OneDeductibleReturned()
		{
			Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
			fundedDeductibles.Add("1", "Deductible01");

			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>();
			fundedDeductiblePolicies.Add("Deductible01", "Policy1");

			string XmlClaim = @"<?xml version='1.0' encoding='utf-16'?>
								<ClaimHeader xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'
								xmlns='http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9'>
								<ClaimTransactionHeaders>
								 </ClaimTransactionHeaders>
								</ClaimHeader>";

			XmlDocument document = new XmlDocument();
			XmlElement xmlNode = null;

			xmlNode = document.CreateElement("Claims"); 
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(XmlClaim);
			XmlNode newNode = document.ImportNode(doc.DocumentElement, true);
			xmlNode.AppendChild(newNode);

			XiapClaim.ClaimTransactionHeader cth = new XiapClaim.ClaimTransactionHeader();
			List<XiapClaim.ClaimTransactionGroup> ctgs = new List<XiapClaim.ClaimTransactionGroup>();

			XiapClaim.ClaimTransactionGroup ctg1 = new XiapClaim.ClaimTransactionGroup();
			ctg1.ClaimTransactionGroupID = 1;
			ctgs.Add(ctg1);

			List<XiapClaim.ClaimTransactionDetail> ctds1 = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.ClaimTransactionDetailID = 1;
			ctd1.MovementType = "A";
			ctds1.Add(ctd1);

			ctg1.ClaimTransactionDetails = ctds1.ToArray();

			cth.ClaimTransactionGroups = ctgs.ToArray();

            XmlNode deductibles = ClaimTransferDataTransform.ExtractDeductibleTransactions(cth, fundedDeductibles, fundedDeductiblePolicies, liabClaimProduct, -1);

			Assert.IsNotNull(deductibles);

			XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
			nsmgr.AddNamespace("XIAP", "http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9");

			XmlNode subNode = deductibles.SelectSingleNode("Deductible01");
			Assert.IsNotNull(subNode);
			XmlAttribute attribute = subNode.Attributes["PolicyReference"];
			Assert.AreEqual("Policy1", attribute.Value);

			subNode = subNode.SelectSingleNode("descendant::XIAP:ClaimTransactionDetail", nsmgr);
			Assert.IsNull(subNode);
		}

        [TestMethod]
        public void ExtractDeductibleTransactions_NoDeductibleCTHOneFundedDedutible_OneDeductibleReturned()
        {
            Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
            fundedDeductibles.Add("1", "Deductible01");

            Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>();
            fundedDeductiblePolicies.Add("Deductible01", "Policy1");

            string XmlClaim = @"<?xml version='1.0' encoding='utf-16'?>
								<ClaimHeader xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'
								xmlns='http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9'>
								<ClaimTransactionHeaders>
								 </ClaimTransactionHeaders>
								</ClaimHeader>";

            XmlDocument document = new XmlDocument();
            XmlElement xmlNode = null;

            xmlNode = document.CreateElement("Claims");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(XmlClaim);
            XmlNode newNode = document.ImportNode(doc.DocumentElement, true);
            xmlNode.AppendChild(newNode);

            XmlNode deductibles = ClaimTransferDataTransform.ExtractDeductibleTransactions(null, fundedDeductibles, fundedDeductiblePolicies, liabClaimProduct, -1);

            Assert.IsNotNull(deductibles);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("XIAP", "http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9");

            XmlNode subNode = deductibles.SelectSingleNode("Deductible01");
            Assert.IsNotNull(subNode);
            XmlAttribute attribute = subNode.Attributes["PolicyReference"];
            Assert.AreEqual("Policy1", attribute.Value);

            subNode = subNode.SelectSingleNode("descendant::XIAP:ClaimTransactionDetail", nsmgr);
            Assert.IsNull(subNode);
        }

		[TestMethod]
		public void ExtractDeductibleTransactions_OneDeductibleCTD_DeductibleReturned()
		{
			Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
			fundedDeductibles.Add("A", "Deductible01");

			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>();
			fundedDeductiblePolicies.Add("Deductible01", "Policy1");

			string XmlClaim = @"<?xml version='1.0' encoding='utf-16'?>
								<ClaimHeader xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'
								xmlns='http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9'>
								<ClaimTransactionHeaders>
								 </ClaimTransactionHeaders>
								</ClaimHeader>";

			XmlDocument document = new XmlDocument();
			XmlElement xmlNode = null;

			xmlNode = document.CreateElement("Claims");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(XmlClaim);
			XmlNode newNode = document.ImportNode(doc.DocumentElement, true);
			xmlNode.AppendChild(newNode);

			XiapClaim.ClaimTransactionHeader cth = new XiapClaim.ClaimTransactionHeader();
			List<XiapClaim.ClaimTransactionGroup> ctgs = new List<XiapClaim.ClaimTransactionGroup>();

			XiapClaim.ClaimTransactionGroup ctg1 = new XiapClaim.ClaimTransactionGroup();
			ctg1.ClaimTransactionGroupID = 1;
			ctgs.Add(ctg1);

			List<XiapClaim.ClaimTransactionDetail> ctds1 = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.ClaimTransactionDetailID = 1;
			ctd1.MovementType = "A";
			ctds1.Add(ctd1);

			ctg1.ClaimTransactionDetails = ctds1.ToArray();

			cth.ClaimTransactionGroups = ctgs.ToArray();

            XmlNode deductibles = ClaimTransferDataTransform.ExtractDeductibleTransactions(cth, fundedDeductibles, fundedDeductiblePolicies, liabClaimProduct, -1);

			Assert.IsNotNull(deductibles);

			XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
			nsmgr.AddNamespace("XIAP", "http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9");

			XmlNode subNode = deductibles.SelectSingleNode("Deductible01");
			Assert.IsNotNull(subNode);
			XmlAttribute attribute = subNode.Attributes["PolicyReference"];
			Assert.AreEqual("Policy1", attribute.Value);

			subNode = subNode.SelectSingleNode("descendant::XIAP:ClaimTransactionDetail", nsmgr);
			Assert.IsNotNull(subNode);

			subNode = subNode.SelectSingleNode("XIAP:MovementType", nsmgr);
			Assert.IsNotNull(subNode);
			Assert.AreEqual("A", subNode.InnerText);
		}

		[TestMethod]
		public void ExtractDeductibleTransactions_MultipleDeductibleCTDMultipleCTG_DeductibleReturned()
		{
			Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
			fundedDeductibles.Add("A", "Deductible01");
			fundedDeductibles.Add("B", "Deductible02");

			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>();
			fundedDeductiblePolicies.Add("Deductible01", "Policy1");
			fundedDeductiblePolicies.Add("Deductible02", "Policy2");

			string XmlClaim = @"<?xml version='1.0' encoding='utf-16'?>
								<ClaimHeader xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'
								xmlns='http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9'>
								<ClaimTransactionHeaders>
								 </ClaimTransactionHeaders>
								</ClaimHeader>";

			XmlDocument document = new XmlDocument();
			XmlElement xmlNode = null;

			xmlNode = document.CreateElement("Claims");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(XmlClaim);
			XmlNode newNode = document.ImportNode(doc.DocumentElement, true);
			xmlNode.AppendChild(newNode);

			XiapClaim.ClaimTransactionHeader cth = new XiapClaim.ClaimTransactionHeader();
			List<XiapClaim.ClaimTransactionGroup> ctgs = new List<XiapClaim.ClaimTransactionGroup>();

			XiapClaim.ClaimTransactionGroup ctg1 = new XiapClaim.ClaimTransactionGroup();
			ctg1.ClaimTransactionGroupID = 1;
			ctgs.Add(ctg1);

			List<XiapClaim.ClaimTransactionDetail> ctds1 = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.ClaimTransactionDetailID = 1;
			ctd1.MovementType = "A";
			ctds1.Add(ctd1);

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.ClaimTransactionDetailID = 2;
			ctd2.MovementType = "B";
			ctds1.Add(ctd2);

			XiapClaim.ClaimTransactionDetail ctd3 = new XiapClaim.ClaimTransactionDetail();
			ctd3.ClaimTransactionDetailID = 3;
			ctd3.MovementType = "A";
			ctds1.Add(ctd3);

			ctg1.ClaimTransactionDetails = ctds1.ToArray();

			XiapClaim.ClaimTransactionGroup ctg2 = new XiapClaim.ClaimTransactionGroup();
			ctg2.ClaimTransactionGroupID = 2;
			ctgs.Add(ctg2);

			List<XiapClaim.ClaimTransactionDetail> ctds2 = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd4 = new XiapClaim.ClaimTransactionDetail();
			ctd4.ClaimTransactionDetailID = 4;
			ctd4.MovementType = "A";
			ctds2.Add(ctd4);

			XiapClaim.ClaimTransactionDetail ctd5 = new XiapClaim.ClaimTransactionDetail();
			ctd5.ClaimTransactionDetailID = 5;
			ctd5.MovementType = "B";
			ctds2.Add(ctd5);

			ctg2.ClaimTransactionDetails = ctds2.ToArray();
			
			cth.ClaimTransactionGroups = ctgs.ToArray();

            XmlNode deductibles = ClaimTransferDataTransform.ExtractDeductibleTransactions(cth, fundedDeductibles, fundedDeductiblePolicies, liabClaimProduct, -1);

			Assert.IsNotNull(deductibles);

			XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
			nsmgr.AddNamespace("XIAP", "http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9");

			XmlNode subNode = deductibles.SelectSingleNode("Deductible01");
			Assert.IsNotNull(subNode);
			XmlAttribute attribute = subNode.Attributes["PolicyReference"];
			Assert.AreEqual("Policy1", attribute.Value);

			XmlNodeList details = subNode.SelectNodes("descendant::XIAP:ClaimTransactionDetail", nsmgr);
			Assert.AreEqual(3, details.Count);

			foreach (XmlNode detail in details)
			{
				XmlNode temp = detail.SelectSingleNode("XIAP:MovementType", nsmgr);
				Assert.IsNotNull(temp);
				Assert.AreEqual("A", temp.InnerText);
			}

			subNode = deductibles.SelectSingleNode("Deductible02");
			Assert.IsNotNull(subNode);
			attribute = subNode.Attributes["PolicyReference"];
			Assert.AreEqual("Policy2", attribute.Value);

			details = subNode.SelectNodes("descendant::XIAP:ClaimTransactionDetail", nsmgr);
			Assert.AreEqual(2, details.Count);

			foreach (XmlNode detail in details)
			{
				XmlNode temp = detail.SelectSingleNode("XIAP:MovementType", nsmgr);
				Assert.IsNotNull(temp);
				Assert.AreEqual("B", temp.InnerText);
			}
		}

		[TestMethod]
		public void ExtractDeductibleTransactions_MultipleDeductibleCTDMultipleCTGOneNonDeductible_DeductibleReturned()
		{
			Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
			fundedDeductibles.Add("A", "Deductible01");
			fundedDeductibles.Add("B", "Deductible02");

			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>();
			fundedDeductiblePolicies.Add("Deductible01", "Policy1");
			fundedDeductiblePolicies.Add("Deductible02", "Policy2");

			string XmlClaim = @"<?xml version='1.0' encoding='utf-16'?>
								<ClaimHeader xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'
								xmlns='http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9'>
								<ClaimTransactionHeaders>
								 </ClaimTransactionHeaders>
								</ClaimHeader>";

			XmlDocument document = new XmlDocument();
			XmlElement xmlNode = null;

			xmlNode = document.CreateElement("Claims");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(XmlClaim);
			XmlNode newNode = document.ImportNode(doc.DocumentElement, true);
			xmlNode.AppendChild(newNode);

			XiapClaim.ClaimTransactionHeader cth = new XiapClaim.ClaimTransactionHeader();
			List<XiapClaim.ClaimTransactionGroup> ctgs = new List<XiapClaim.ClaimTransactionGroup>();

			XiapClaim.ClaimTransactionGroup ctg1 = new XiapClaim.ClaimTransactionGroup();
			ctg1.ClaimTransactionGroupID = 1;
			ctgs.Add(ctg1);

			List<XiapClaim.ClaimTransactionDetail> ctds1 = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail();
			ctd1.ClaimTransactionDetailID = 1;
			ctd1.MovementType = "A";
			ctds1.Add(ctd1);

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.ClaimTransactionDetailID = 2;
			ctd2.MovementType = "B";
			ctds1.Add(ctd2);

			XiapClaim.ClaimTransactionDetail ctd3 = new XiapClaim.ClaimTransactionDetail();
			ctd3.ClaimTransactionDetailID = 3;
			ctd3.MovementType = "A";
			ctds1.Add(ctd3);

			XiapClaim.ClaimTransactionDetail ctd4 = new XiapClaim.ClaimTransactionDetail();
			ctd4.ClaimTransactionDetailID = 4;
			ctd4.MovementType = "C";
			ctds1.Add(ctd4);

			ctg1.ClaimTransactionDetails = ctds1.ToArray();

			XiapClaim.ClaimTransactionGroup ctg2 = new XiapClaim.ClaimTransactionGroup();
			ctg2.ClaimTransactionGroupID = 2;
			ctgs.Add(ctg2);

			List<XiapClaim.ClaimTransactionDetail> ctds2 = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd5 = new XiapClaim.ClaimTransactionDetail();
			ctd5.ClaimTransactionDetailID = 4;
			ctd5.MovementType = "A";
			ctds2.Add(ctd5);

			XiapClaim.ClaimTransactionDetail ctd6 = new XiapClaim.ClaimTransactionDetail();
			ctd6.ClaimTransactionDetailID = 5;
			ctd6.MovementType = "B";
			ctds2.Add(ctd6);

			ctg2.ClaimTransactionDetails = ctds2.ToArray();

			cth.ClaimTransactionGroups = ctgs.ToArray();

            XmlNode deductibles = ClaimTransferDataTransform.ExtractDeductibleTransactions(cth, fundedDeductibles, fundedDeductiblePolicies, liabClaimProduct, -1);

			Assert.IsNotNull(deductibles);

			XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
			nsmgr.AddNamespace("XIAP", "http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9");

			XmlNode subNode = deductibles.SelectSingleNode("Deductible01");
			Assert.IsNotNull(subNode);
			XmlAttribute attribute = subNode.Attributes["PolicyReference"];
			Assert.AreEqual("Policy1", attribute.Value);

			XmlNodeList details = subNode.SelectNodes("descendant::XIAP:ClaimTransactionDetail", nsmgr);
			Assert.AreEqual(3, details.Count);

			foreach (XmlNode detail in details)
			{
				XmlNode temp = detail.SelectSingleNode("XIAP:MovementType", nsmgr);
				Assert.IsNotNull(temp);
				Assert.AreEqual("A", temp.InnerText);
			}

			subNode = deductibles.SelectSingleNode("Deductible02");
			Assert.IsNotNull(subNode);
			attribute = subNode.Attributes["PolicyReference"];
			Assert.AreEqual("Policy2", attribute.Value);

			details = subNode.SelectNodes("descendant::XIAP:ClaimTransactionDetail", nsmgr);
			Assert.AreEqual(2, details.Count);

			foreach (XmlNode detail in details)
			{
				XmlNode temp = detail.SelectSingleNode("XIAP:MovementType", nsmgr);
				Assert.IsNotNull(temp);
				Assert.AreEqual("B", temp.InnerText);
			}
		}

		[TestMethod]
		public void ExtractDeductibleTransactions_MultipleDeductibleCTDOneCTGDeductibleOneNoCTG_DeductibleReturned()
		{
			Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
			fundedDeductibles.Add("A", "Deductible01");
			fundedDeductibles.Add("B", "Deductible02");

			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>();
			fundedDeductiblePolicies.Add("Deductible01", "Policy1");
			fundedDeductiblePolicies.Add("Deductible02", "Policy2");

			string XmlClaim = @"<?xml version='1.0' encoding='utf-16'?>
								<ClaimHeader xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'
								xmlns='http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9'>
								<ClaimTransactionHeaders>
								 </ClaimTransactionHeaders>
								</ClaimHeader>";

			XmlDocument document = new XmlDocument();
			XmlElement xmlNode = null;

			xmlNode = document.CreateElement("Claims");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(XmlClaim);
			XmlNode newNode = document.ImportNode(doc.DocumentElement, true);
			xmlNode.AppendChild(newNode);

			XiapClaim.ClaimTransactionHeader cth = new XiapClaim.ClaimTransactionHeader();
			List<XiapClaim.ClaimTransactionGroup> ctgs = new List<XiapClaim.ClaimTransactionGroup>();

			XiapClaim.ClaimTransactionGroup ctg1 = new XiapClaim.ClaimTransactionGroup();
			ctg1.ClaimTransactionGroupID = 1;
			ctgs.Add(ctg1);

			List<XiapClaim.ClaimTransactionDetail> ctds1 = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail();
			ctd2.ClaimTransactionDetailID = 2;
			ctd2.MovementType = "B";
			ctds1.Add(ctd2);

			XiapClaim.ClaimTransactionDetail ctd4 = new XiapClaim.ClaimTransactionDetail();
			ctd4.ClaimTransactionDetailID = 4;
			ctd4.MovementType = "C";
			ctds1.Add(ctd4);

			ctg1.ClaimTransactionDetails = ctds1.ToArray();

			XiapClaim.ClaimTransactionGroup ctg2 = new XiapClaim.ClaimTransactionGroup();
			ctg2.ClaimTransactionGroupID = 2;
			ctgs.Add(ctg2);

			List<XiapClaim.ClaimTransactionDetail> ctds2 = new List<XiapClaim.ClaimTransactionDetail>();

			XiapClaim.ClaimTransactionDetail ctd6 = new XiapClaim.ClaimTransactionDetail();
			ctd6.ClaimTransactionDetailID = 5;
			ctd6.MovementType = "B";
			ctds2.Add(ctd6);

			ctg2.ClaimTransactionDetails = ctds2.ToArray();

			cth.ClaimTransactionGroups = ctgs.ToArray();

            XmlNode deductibles = ClaimTransferDataTransform.ExtractDeductibleTransactions(cth, fundedDeductibles, fundedDeductiblePolicies, liabClaimProduct, -1);

			Assert.IsNotNull(deductibles);

			XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
			nsmgr.AddNamespace("XIAP", "http://www.xchanging.com/Xiap/Claims/Data/XML/2011/9");

			XmlNode subNode = deductibles.SelectSingleNode("Deductible01");
			Assert.IsNotNull(subNode);
			XmlAttribute attribute = subNode.Attributes["PolicyReference"];
			Assert.AreEqual("Policy1", attribute.Value);

			XmlNodeList details = subNode.SelectNodes("descendant::XIAP:ClaimTransactionDetail", nsmgr);
			Assert.AreEqual(0, details.Count);

			subNode = deductibles.SelectSingleNode("Deductible02");
			Assert.IsNotNull(subNode);
			attribute = subNode.Attributes["PolicyReference"];
			Assert.AreEqual("Policy2", attribute.Value);

			details = subNode.SelectNodes("descendant::XIAP:ClaimTransactionDetail", nsmgr);
			Assert.AreEqual(2, details.Count);

			foreach (XmlNode detail in details)
			{
				XmlNode temp = detail.SelectSingleNode("XIAP:MovementType", nsmgr);
				Assert.IsNotNull(temp);
				Assert.AreEqual("B", temp.InnerText);
			}
		}

		[TestMethod]
		public void IsFundedDeductibleType_ReferenceIsBlank_FalseReturned()
		{
			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>()
			{
				{ "Deductible01", String.Empty },
			};

			string attachedPolicyRef = "0000000000SE00A";
			bool result = ClaimTransferDataTransform.IsFundedDeductibleType("Deductible01", fundedDeductiblePolicies, attachedPolicyRef);
			Assert.IsFalse(result);
		}

		[TestMethod]
		public void IsFundedDeductibleType_ReferenceEqualToAttached_FalseReturned()
		{
			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>()
			{
				{ "Deductible01", "0000000000SE00A" },
			};

			string attachedPolicyRef = "0000000000SE00A";
			bool result = ClaimTransferDataTransform.IsFundedDeductibleType("Deductible01", fundedDeductiblePolicies, attachedPolicyRef);
			Assert.IsFalse(result);
		}

		[TestMethod]
		public void IsFundedDeductibleType_ReferenceIsNotService_TrueReturned()
		{
			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>()
			{
				{ "Deductible01", "0000000000XX00A" },
			};

			string attachedPolicyRef = "0000000000SE00A";
			bool result = ClaimTransferDataTransform.IsFundedDeductibleType("Deductible01", fundedDeductiblePolicies, attachedPolicyRef);
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void IsFundedDeductibleType_ReferenceIsService_TrueReturned()
		{
			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>()
			{
				{ "Deductible01", "0000000000SE00A" },
			};

			string attachedPolicyRef = "0000000000XX00A";
			bool result = ClaimTransferDataTransform.IsFundedDeductibleType("Deductible01", fundedDeductiblePolicies, attachedPolicyRef);
			Assert.IsTrue(result);
		}

        [TestMethod]
        public void IsReserveWithNonZeroMovementsPresentTest()
        {
            List<XiapClaim.ClaimTransactionDetail> ctds = new List<XiapClaim.ClaimTransactionDetail>();
            XiapClaim.ClaimTransactionDetail ctd1 = new XiapClaim.ClaimTransactionDetail() { AmountType = (short)StaticValues.AmountType.Payment, TransactionAmountOriginal = 0, MovementType = "A" };
            ctds.Add(ctd1);
            XiapClaim.ClaimTransactionDetail ctd2 = new XiapClaim.ClaimTransactionDetail() { AmountType = (short)StaticValues.AmountType.Reserve, MovementAmountOriginal = 1, MovementType = "B" };
            ctds.Add(ctd2);
            Assert.AreEqual(true, ClaimTransferDataTransform.IsReserveWithNonZeroMovementsPresent(ctds));
        }
	}
}
