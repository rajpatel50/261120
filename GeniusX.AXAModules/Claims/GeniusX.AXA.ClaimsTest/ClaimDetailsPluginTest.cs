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
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ClaimDetailsPluginTest : ComponentPluginBaseTest<ClaimDetailsPlugin>
    {
        protected const long VALID_CLAIM_PRODUCT_VERSIONID = -8;
        private ClaimDetail component;
        private ClaimHeader header;
        private ProductClaimDetailToComponentLink productClaimDetailToComponentLink;
        private ClaimDetailToClaimInvolvementLink claimDetailToClaimInvolvementLink;

        [TestInitialize]
        public void TestInitialize()
        {
            this.component = new ClaimDetail() { ClaimDetailTypeCode = "AD", PolicyCoverageID = -2, PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage };

            ProductClaimDetail pcd = new ProductClaimDetail();

            this.header = new ClaimHeader();
            this.header.CustomBoolean03 = false;
            this.header.InternalClaimDetails.Add(this.component);
            this.component.ClaimHeader.InternalClaimDetails.Add(new ClaimDetail() { ClaimDetailReference = "D0001" });

            var context = this.component.Parent.Context;

            pcd.ProductClaimDetailID = 1;
            pcd.ProductVersion = new ProductVersion();
            pcd.ProductVersion.Product = new Product();
            pcd.ProductVersion.ProductClaimDefinition = new ProductClaimDefinition() { ClaimDetailAttachmentChangePreventionThreshold = -1 };
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
            var list = new List<short?>() { 1 };

            container.RegisterInstance<IMetadataQuery>(metadataMock);
            IConfigurationManager icm = MockRepository.GenerateStub<IConfigurationManager>();

            container.RegisterInstance<IConfigurationManager>(icm);

            NameValueCollection appsetting = new NameValueCollection();
            icm.Stub(c => c.AppSettings).Return(appsetting);

            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            IInsuranceDirectoryQuery insuranceQuery = MockRepository.GenerateStub<IInsuranceDirectoryQuery>();
            Name nameSample = new Name();
            nameSample.NameType = (short)StaticValues.NameType.Person;
            nameSample.NameID = -1;
            nameSample.PersonDetailVersion = new EntityCollection<PersonDetailVersion>();
            nameSample.PersonDetailVersion.Add(new PersonDetailVersion() { ListName = "IDS Test" });
            insuranceQuery.Stub(x => x.GetName(Arg<long>.Is.Anything)).Return(nameSample);
            container.RegisterInstance<IInsuranceDirectoryQuery>(insuranceQuery);
            ObjectFactory.Instance = new ObjectFactory(container);

            ClaimInvolvement claimInvolvement = new ClaimInvolvement();
            claimInvolvement.ClaimInvolvementType = 1;

            this.claimDetailToClaimInvolvementLink = new ClaimDetailToClaimInvolvementLink();
            this.claimDetailToClaimInvolvementLink.ClaimInvolvement = claimInvolvement;

            this.header.InternalClaimInvolvements.Add(claimInvolvement);

            this.component.InternalClaimDetailToClaimInvolvementLinks = new EntityCollection<ClaimDetailToClaimInvolvementLink>();
            this.component.InternalClaimDetailToClaimInvolvementLinks.Add(this.claimDetailToClaimInvolvementLink);

            ObjectFactory.Resolve<IMetadataQuery>().Stub(m => m.GetProductClaimDetailToComponentLinks(new List<long>() { 1 })).Return(new List<ProductClaimDetailToComponentLink>() { this.productClaimDetailToComponentLink });
            this.component.Context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);

            ProductXML.ProductClaimDetail productClaimDetail = new ProductXML.ProductClaimDetail { ProductClaimDetailID = 1 };
            productClaimDetail.ProductClaimDetailToComponentLinks = new ProductXML.ProductClaimDetailToComponentLink[1]
                                                                    {
                                                                        new ProductXML.ProductClaimDetailToComponentLink() {ProductLinkableComponentID =1 }
                                                                    };

            List<ProductXML.ProductClaimDetail> productClaimDetails = new List<ProductXML.ProductClaimDetail> { productClaimDetail };
            IProductClaimDetailQuery claimDetailQuery = MockRepository.GenerateStub<IProductClaimDetailQuery>();
            container.RegisterInstance<IProductClaimDetailQuery>(claimDetailQuery);
            claimDetailQuery.Stub(x => x.GetProductClaimDetails(Arg<long>.Is.Anything)).Return(productClaimDetails);
            claimDetailQuery.Stub(x => x.GetProductClaimDetail(Arg<long>.Is.Anything)).Return(productClaimDetails.First());
            claimDetailQuery.Stub(x => x.GetProductClaimDetail(Arg<long>.Is.Anything, Arg<string>.Is.Anything)).Return(productClaimDetails.First());
        }

        [TestMethod]
        public void SetClaimDetailTitleTest()
        {
            target = new ClaimDetailsPlugin();
            ClaimNameInvolvement ni = new ClaimNameInvolvement() { NameInvolvementType = (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant, NameUsageTypeCode = "CLMT", NameID = -1, NameInvolvementMaintenanceStatus = (short?)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest };
            this.header.InternalClaimInvolvements.First().InternalClaimNameInvolvements.Add(ni);
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual("AD - IDS Test", this.component.ClaimDetailTitle);
        }

        [TestMethod]
        public void InitializeExcess_CopyValuesFromClaimHeader_ValuesSet()
        {
            target = new ClaimDetailsPlugin();
            this.header.CustomBoolean15 = true;
            this.header.CustomNumeric10 = 100;

            ClaimDetail claimDetail = new ClaimDetail();
            claimDetail.IsAutomaticDeductibleProcessingApplied = true;
            claimDetail.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod = (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible;

            this.header.InternalClaimDetails.Add(claimDetail);

            target.ProcessComponent(claimDetail, ProcessInvocationPoint.Created, 0);

            Assert.AreEqual(true, claimDetail.IsDeductible01PaidByInsurer);
            Assert.AreEqual(100, claimDetail.PolicyDeductible01);
        }

        private bool HasNonZeroReserve(ClaimDetail component, short p)
        {
            return false;
        }

        private void SetContext(ClaimHeader claimHeader)
        {
            var context = new ClaimsTransactionContext(string.Empty, string.Empty, string.Empty);
            context.ProductVersionID = VALID_CLAIM_PRODUCT_VERSIONID;
            claimHeader.Visit((IBusinessComponent comp) => { comp.Context = context; return true; }, null, true);
        }

        [TestMethod]
        public void AttacheAllClaimDetailsToCoverageTest()
        {
            target = new ClaimDetailsPlugin();
            ClaimDetail claimDetail = new ClaimDetail() { ClaimDetailTypeCode = "AD" };
            claimDetail.PropertiesChanged = new Dictionary<string, ChangedProperty>();
            ChangedProperty cp = new ChangedProperty(10, 11);
            claimDetail.PropertiesChanged.Add("PolicyCoverageID", cp);

            ChangedProperty cp2 = new ChangedProperty(null, 4);
            claimDetail.PropertiesChanged.Add("PolicyLinkLevel", cp2);

            ClaimDetail claimDetail2 = new ClaimDetail() { ClaimDetailTypeCode = "TPI", ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.InProgress };
            ClaimDetail claimDetail3 = new ClaimDetail() { ClaimDetailTypeCode = "TPI", ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.Finalized };
            ClaimDetail claimDetail4 = new ClaimDetail() { ClaimDetailTypeCode = "TPI", ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.ClosedCreatedinError };
            claimDetail.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod = (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible;
            this.header.InternalClaimDetails.Add(claimDetail);
            this.header.InternalClaimDetails.Add(claimDetail2);
            this.header.InternalClaimDetails.Add(claimDetail3);
            this.header.InternalClaimDetails.Add(claimDetail4);
            target.ProcessComponent(claimDetail, ProcessInvocationPoint.ComponentChange, 0);

            Assert.AreEqual(claimDetail.PolicyCoverageID, claimDetail2.PolicyCoverageID);
            Assert.AreEqual(claimDetail.PolicyLinkLevel, claimDetail2.PolicyLinkLevel);

            Assert.AreEqual(claimDetail.PolicyCoverageID, claimDetail3.PolicyCoverageID);
            Assert.AreEqual(claimDetail.PolicyLinkLevel, claimDetail3.PolicyLinkLevel);

            Assert.AreEqual(claimDetail.PolicyCoverageID, claimDetail4.PolicyCoverageID);
            Assert.AreEqual(claimDetail.PolicyLinkLevel, claimDetail4.PolicyLinkLevel);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
