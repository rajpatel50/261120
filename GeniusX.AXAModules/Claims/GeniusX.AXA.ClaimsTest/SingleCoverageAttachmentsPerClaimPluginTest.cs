using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Configuration;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class SingleCoverageAttachmentsPerClaimPluginTest : ComponentPluginBaseTest<SingleCoverageAttachmentsPerClaimPlugin>
    {
        private ClaimDetail component;
        private void TestInitializeVariables(string dateOfLossTypeCode)
        {
            target = new SingleCoverageAttachmentsPerClaimPlugin();

            ClaimHeader claimHeader = new ClaimHeader();
            claimHeader.DateOfLossFrom = DateTime.Now;
            this.component.OriginalClaimDetailDataID = null;
            this.component.PropertiesChanged.Add(ClaimDetail.PolicyLinkLevelFieldName, new ChangedProperty("ABC", "TEST"));
            claimHeader.InternalClaimDetails.Add(this.component);

            DirtyProperty dt = new DirtyProperty(null, "XUK0020850MO11A");
            DirtyProperty dt1 = new DirtyProperty(null, "3");
            claimHeader.DirtyPropertyList.Add("PolicyHeaderID", dt);
            claimHeader.DirtyPropertyList.Add("PolicyLinkLevel", dt1);

            UWHeaderData uwHeaderData = new UWHeaderData();
            UWSectionData sectionData = new UWSectionData() { SectionStartDate = DateTime.MinValue, SectionEndDate = DateTime.MaxValue };
            UWSectionDetailData sectionDetailData = new UWSectionDetailData() { CustomCode03 = dateOfLossTypeCode, SectionDetailStartDate = DateTime.MinValue, SectionDetailEndDate = DateTime.MaxValue };
            UWCoverageData coverageData = new UWCoverageData() { CoverageTitle = "ABC", CoverageStartDate = DateTime.MinValue, CoverageEndDate = DateTime.MaxValue, ExternalReference = "10011100" };
            NameInvolvementData nameInvData = new NameInvolvementData() { NameUsageTypeCode = "INS", NameID = 36 };
            UWTermsData termsData = new UWTermsData() { VersionNumber = 1, TermsStartDate = DateTime.MinValue, TermsEndDate = DateTime.MaxValue };

            claimHeader.UWHeader = uwHeaderData;
            claimHeader.UWHeader.CustomReference09 = "GBP";
            claimHeader.UWHeader.CustomReference10 = "GBP";

            uwHeaderData.Sections = new List<UWSectionData>();
            uwHeaderData.Sections.Add(sectionData);

            sectionData.SectionDetails = new List<UWSectionDetailData>();
            sectionData.SectionDetails.Add(sectionDetailData);

            sectionDetailData.Coverages = new List<UWCoverageData>();
            sectionDetailData.Coverages.Add(coverageData);

            uwHeaderData.Nameinvolvements = new List<NameInvolvementData>();
            uwHeaderData.Nameinvolvements.Add(nameInvData);

            uwHeaderData.Terms = new List<UWTermsData>();
            uwHeaderData.Terms.Add(termsData);

            ProductClaimTransaction productClaimTransaction = new ProductClaimTransaction() { ProductClaimTransactionID = 1 };

            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
  
            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            metadataMock.Stub(m => m.GetProductClaimTransaction(Arg<long>.Is.Anything)).Return(productClaimTransaction);
            var list = new List<short?>() { 1 };

            IUWSectionDetail sectionDetail = new SectionDetailVersion();
            IUnderwritingService mockIUnderwritingService = MockRepository.GenerateStub<IUnderwritingService>();
            mockIUnderwritingService.Stub(m => m.GetPolicySectionDetails(Arg<long>.Is.Anything)).Return(sectionDetail);
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            container.RegisterInstance<IUnderwritingService>("XIAP", mockIUnderwritingService);

            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(configurationManager);

            ObjectFactory.Instance = new ObjectFactory(container);
        }

        [TestMethod]
        public void SetDateOfLossTypeTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage,
                PolicyCoverageID = 235,
                UWCoverage = new UWCoverageData() { CoverageTitle = "ABC", CoverageStartDate = DateTime.MinValue, CoverageEndDate = DateTime.MaxValue, ExternalReference = "10011100" }
            };

            this.TestInitializeVariables("O");
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            ProcessResultsCollection results = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual("O", this.component.ParentClaimHeader.DateOfLossTypeCode);
        }

        [TestMethod]
        public void SetDefaultValueOfDateOfLossTypeTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage,
                PolicyCoverageID = 235,
                UWCoverage = new UWCoverageData() { CoverageTitle = "ABC", CoverageStartDate = DateTime.MinValue, CoverageEndDate = DateTime.MaxValue, ExternalReference = "10011100" }
            };
            this.TestInitializeVariables(null);
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual("O", this.component.ParentClaimHeader.DateOfLossTypeCode);
        }

        [TestMethod]
        public void ValidateSectionStartAndEndDateErrorAtComponentCreationTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Section,
                PolicySectionID = 236,
                UWSection = new UWSectionData() { SectionStartDate = DateTime.Now.AddDays(1), SectionEndDate = DateTime.Now },
            };

            this.TestInitializeVariables("O");
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            ProcessResultsCollection results = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(null, this.component.PolicySectionID);
            Assert.AreEqual(null, this.component.PolicyLinkLevel);
            Assert.AreEqual(ClaimConstants.DATE_OUTSIDE_THE_COVER_PERIOD, results.Results.FirstOrDefault().MessageTitle);
        }

        [TestMethod]
        public void ValidateSectionDetailStartAndEndDateErrorAtComponentCreationTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.SectionDetail,
                PolicySectionDetailID = 237,
                UWSectionDetail = new UWSectionDetailData() { SectionDetailStartDate = DateTime.Now.AddDays(1), SectionDetailEndDate = DateTime.Now },
            };

            this.TestInitializeVariables("O");
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            ProcessResultsCollection results = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(null, this.component.PolicySectionDetailID);
            Assert.AreEqual(null, this.component.PolicyLinkLevel);
            Assert.AreEqual(ClaimConstants.DATE_OUTSIDE_THE_COVER_PERIOD, results.Results.FirstOrDefault().MessageTitle);
        }

        [TestMethod]
        public void ValidateCoverageStartAndEndDateErrorAtComponentCreationTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage,
                PolicyCoverageID = 235,
                UWCoverage = new UWCoverageData() { CoverageTitle = "ABC", CoverageStartDate = DateTime.Now.AddDays(1), CoverageEndDate = DateTime.Now, ExternalReference = "10011100" }
            };

            this.TestInitializeVariables("O");
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            ProcessResultsCollection results = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(null, this.component.PolicyCoverageID);
            Assert.AreEqual(null, this.component.PolicyLinkLevel);
            Assert.AreEqual(ClaimConstants.DATE_OUTSIDE_THE_COVER_PERIOD, results.Results.FirstOrDefault().MessageTitle);
        }

        [TestMethod]
        public void ValidatePaymentExistErrorTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage,
                PolicyCoverageID = 235,
                UWCoverage = new UWCoverageData() { CoverageTitle = "ABC", CoverageStartDate = DateTime.MinValue, CoverageEndDate = DateTime.MaxValue, ExternalReference = "10011100" }
            };

            this.TestInitializeVariables(null);
            ClaimHeader clmHeader = this.component.ClaimHeader;
            clmHeader.ClaimProductVersionID = 1;
            clmHeader.AddNewClaimTransactionHeader(new Xiap.Claims.Data.ClaimTransactionHeaderData()
            {
                ProductClaimTransactionID = 1,
                ClaimTransactionSource = (short)StaticValues.ClaimTransactionSource.Payment,
                IsMultiClaimDetailTransaction = true,
            });
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            ProcessResultsCollection results = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(null, this.component.PolicyCoverageID);
            Assert.AreEqual(null, this.component.PolicyLinkLevel);
            Assert.AreEqual(ClaimConstants.PAYMENT_ENTERED_POLICY_TRIGGER_TYPE_CHANGE_NOT_ALLOWED, results.Results.FirstOrDefault().MessageTitle);
        }

        [TestMethod]
        public void LinkPolicytoClaimDetailsTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage,
                PolicyCoverageID = 235
            };

            target = new SingleCoverageAttachmentsPerClaimPlugin();
            this.TestInitializeVariables(null);
            ClaimDetail claimDetail = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                UWCoverage = new UWCoverageData() { CoverageTitle = "ABC" }
            };

            this.component.ClaimHeader.InternalClaimDetails.Add(claimDetail);

            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            target.ProcessComponent(claimDetail, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(this.component.PolicyLinkLevel, claimDetail.PolicyLinkLevel);
            Assert.AreEqual(this.component.PolicyCoverageID, claimDetail.PolicyCoverageID);
        }

        [TestMethod]
        public void ValidateAgainstExistingClaimDetailsAtComponentChangeTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage,
                PolicyCoverageID = 235,
            };

            target = new SingleCoverageAttachmentsPerClaimPlugin();
            this.TestInitializeVariables(null);
            ClaimDetail claimDetail = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage,
                PolicyCoverageID = 237
            };

            this.component.ClaimHeader.InternalClaimDetails.Add(claimDetail);

            ProcessInvocationPoint point = ProcessInvocationPoint.ComponentChange;
            ProcessResultsCollection result = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(ClaimConstants.COVERAGE_IS_ALREADY_ATTACHED, result.Results.First().MessageId);
        }

        [TestMethod]
        public void ValidateSectionDetailStartAndEndDateErrorAtComponentChangeTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.SectionDetail,
                PolicySectionDetailID = 237,
                UWSectionDetail = new UWSectionDetailData() { SectionDetailStartDate = DateTime.Now.AddDays(1), SectionDetailEndDate = DateTime.Now },
            };

            this.TestInitializeVariables("O");
            ProcessInvocationPoint point = ProcessInvocationPoint.ComponentChange;
            ProcessResultsCollection results = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(null, this.component.PolicySectionDetailID);
            Assert.AreEqual(null, this.component.PolicyLinkLevel);
            Assert.AreEqual(ClaimConstants.DATE_OUTSIDE_THE_COVER_PERIOD, results.Results.FirstOrDefault().MessageTitle);
        }

        [TestMethod]
        public void ValidateCoverageStartAndEndDateErrorAtComponentChangeTest()
        {
            this.component = new ClaimDetail()
            {
                ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress,
                PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage,
                PolicyCoverageID = 235,
                UWCoverage = new UWCoverageData() { CoverageTitle = "ABC", CoverageStartDate = DateTime.Now.AddDays(1), CoverageEndDate = DateTime.Now, ExternalReference = "10011100" }
            };

            this.TestInitializeVariables("O");
            ProcessInvocationPoint point = ProcessInvocationPoint.ComponentChange;
            ProcessResultsCollection results = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(null, this.component.PolicyCoverageID);
            Assert.AreEqual(null, this.component.PolicyLinkLevel);
            Assert.AreEqual(ClaimConstants.DATE_OUTSIDE_THE_COVER_PERIOD, results.Results.FirstOrDefault().MessageTitle);
        }
    }
}
