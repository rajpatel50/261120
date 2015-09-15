using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common.Product;
using Xiap.Framework.Configuration;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.Framework.Data.Underwriting;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class PolicyAttachmentAndDateProcessingForClaimDetailTest : ComponentPluginBaseTest<PolicyAttachmentAndDateProcessingForClaimDetail>
    {
        protected const long VALID_CLAIM_PRODUCT_VERSIONID = 8;
        private const string CLAIM_DETAIL_TYPE_CODE = "LIA";
        private ClaimDetail component;
        private ClaimHeader header;
      
        [TestInitialize]
        public void TestInitialize()
        {
            this.component = new ClaimDetail() { ClaimDetailTypeCode = "AD", PolicyCoverageID = -2, PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage, ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress };

            ProductClaimDetail pcd = new ProductClaimDetail();

            this.header = new ClaimHeader();
            this.header.CustomBoolean03 = false;
            this.header.ClaimProductVersionID = 1;
            this.header.PolicyExternalDataSource = "GENIUS";
            this.header.InternalClaimDetails.Add(this.component);
            this.component.ClaimHeader.InternalClaimDetails.Add(new ClaimDetail() { ClaimDetailReference = "D0001" });
            UWHeaderData uwHeaderData = new UWHeaderData() { HeaderReference = "XUK000512612MO", HeaderID = 2 };
            UWSectionData sectionData = new UWSectionData();
            UWSectionDetailData sectionDetailData = new UWSectionDetailData() { CustomCode03 = "O" };
            UWCoverageData coverageData = new UWCoverageData() { CoverageTitle = "ABC" };
            UWTermsData termsData = new UWTermsData() { VersionNumber = 1, TermsStartDate = DateTime.MinValue, TermsEndDate = DateTime.MaxValue };
            this.component.UWSection = sectionData;
            this.component.UWSectionDetail = sectionDetailData;
            this.component.UWCoverage = coverageData;
            this.header.UWHeader = uwHeaderData;
            this.header.PolicyExternalDataSource = "GENIUS";
            pcd.ProductClaimDetailID = 1;
            pcd.ProductVersion = new ProductVersion();
            pcd.ProductVersion.Product = new Product();
            pcd.ProductVersion.ProductClaimDefinition = new ProductClaimDefinition() { ClaimDetailAttachmentChangePreventionThreshold = -1, ClaimHeaderAutomaticDeductibleProcessingMethod = (short?)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.StandardClaimHeaderDeductible };
            pcd.ProductVersion.Product.Code = "AD";

            pcd.ProductVersion.ProductLink.Add(new ProductLink { ProductLinkType = (short)StaticValues.ProductLinkType.ClaimtoPolicy, IsClaimLinkToSectionAllowed = true, IsClaimLinkToSectionDetailAllowed = true, IsClaimLinkToCoverageAllowed = true });

            pcd.ClaimDetailType = new ClaimDetailType();
            pcd.ClaimDetailType.Code = "TYPE";
            this.component.ProductClaimDetailID = pcd.ProductClaimDetailID;

            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();
            metadata.Stub(m => m.GetDefinitionComponent<ProductClaimDetail>()).Return(pcd);
            metadata.Stub(m => m.GetDefinitionComponent<ProductVersion>()).Return(pcd.ProductVersion);
            metadata.Stub(m => m.DefinitionComponent).Return(pcd);

            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IComponentMetadata>(metadata);

            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();

            container.RegisterInstance<IMetadataQuery>(metadataMock);

            IAXAClaimsQuery claimsQuery = MockRepository.GenerateStub<IAXAClaimsQuery>();
            claimsQuery.Stub(a => a.GetHeaderStatus(Arg<string>.Is.Anything)).Return("CLM");
            container.RegisterInstance<IAXAClaimsQuery>(claimsQuery);
            ObjectFactory.Instance = new ObjectFactory(container);
        }

        [TestMethod]
        public void PreCreateValidateWithError()
        {
            target = new PolicyAttachmentAndDateProcessingForClaimDetail();
            ProcessResultsCollection results = target.ProcessComponent(this.component, ProcessInvocationPoint.PreCreateValidation, 0);
            Assert.IsTrue(MessageIdExists(results, ClaimConstants.POLICYNOTVERIFIED_COVERAGEVERIFICATION_NOTALLOWED));
        }

        private static bool MessageIdExists(ProcessResultsCollection results, string messageId)
        {
            foreach (ProcessResult result in results.Results)
            {
                if (result.MessageId == messageId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
