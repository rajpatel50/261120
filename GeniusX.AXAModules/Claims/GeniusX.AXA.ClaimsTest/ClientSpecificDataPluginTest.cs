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
using Xiap.Framework.Common.Product;
using Xiap.Framework.Configuration;
using Xiap.Framework.Data.InsuranceDirectory;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.UW.BusinessComponent;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ClientSpecificDataPluginTest : ComponentPluginBaseTest<ClientSpecificDataPlugin>
    {
        protected const long VALID_CLAIM_PRODUCT_VERSIONID = 3;
        protected const long VALID_UW_PRODUCT_VERSIONID = 3;
        private const string CLAIM_PRODUCT_CODE = "LIABCLAIM";
        private const string CLAIM_HEADER_STATUS_CODE = "CON";
        private const string CLAIM_DETAIL_TYPE_CODE = "LIA";
        private const StaticValues.NameInvolvementType INSURED_NAMEINVOLVEMENT_TYPE = StaticValues.NameInvolvementType.MajorInsured;
        private const string INSURED_NAMEUSAGE_TYPE_CODE = "INS";
        private ClaimHeader claimHeader;
        private ClaimNameInvolvement component;
        private ProductClaimDetail pcd = null;

        private ClaimNameInvolvement AddInsuredNameInvolvement()
        {
            ClaimNameInvolvement nameInvolvement = new ClaimNameInvolvement();
            nameInvolvement.NameID = 1;
            nameInvolvement.NameInvolvementType = (short)StaticValues.NameInvolvementType.MajorInsured;
            nameInvolvement.NameUsageTypeCode = "INS";
            nameInvolvement.NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest;
            ClaimInvolvement invo = new ClaimInvolvement();
            invo.InternalClaimNameInvolvements.Add(nameInvolvement);
            this.claimHeader.InternalClaimInvolvements.Add(invo);
            return nameInvolvement;
        }

        private Header GetPolicyHeader()
        {
            Header header = new Header(VALID_CLAIM_PRODUCT_VERSIONID, DateTime.Today.AddMonths(-6));

            HeaderVersion hv = new HeaderVersion();
            hv.HeaderTitle = "ClientSpecificCode Test";
            hv.InceptionDate = DateTime.Today.AddMonths(-6);
            hv.IsLatestVersion = true;
            header.HeaderVersion.Add(hv);

            UwNameInvolvement uwNameInvolvement = new UwNameInvolvement();
            uwNameInvolvement.NameID = 1;
            uwNameInvolvement.NameInvolvementType = (short)StaticValues.NameInvolvementType.MajorInsured;
            uwNameInvolvement.NameUsageTypeCode = "INS";

            UwNameInvolvementVersion uwNameInvVersion = new UwNameInvolvementVersion();
            uwNameInvVersion.IsLatestVersion = true;
            uwNameInvolvement.UwNameInvolvementVersion.Add(uwNameInvVersion);
            header.UwNameInvolvement.Add(uwNameInvolvement);
            if (string.IsNullOrEmpty(uwNameInvolvement.NameUsageTypeCode))
            {
                uwNameInvolvement.NameUsageTypeCode = INSURED_NAMEUSAGE_TYPE_CODE;
            }

            return header;
        }

        private INameUsage GetNameUsage()
        {
            NameUsage nu = new NameUsage("INS");
            nu.CustomCode01 = "BT";
            return nu;
        }

        private void SetContext(ClaimHeader claimHeader)
        {
            var context = new ClaimsTransactionContext(string.Empty, string.Empty, string.Empty);
            context.ProductVersionID = VALID_CLAIM_PRODUCT_VERSIONID;
            claimHeader.Visit((IBusinessComponent comp) => { comp.Context = context; return true; }, null, true);
        }

        private IMetadataQuery GetMockData(out ProductVersion productVersion)
        {
            GenericDataType gdt = new GenericDataType { Code = "UTGDT", CustomCode01 = "BT" };
            GenericDataTypeNames gdtNames = new GenericDataTypeNames { LanguageID = 1, LongDescription = "ClientSpecficTST", GenericDataType = gdt };
            GenericDataTypeVersion gdtVersion = new GenericDataTypeVersion { GenericDataTypeVersionID = 1, GenericDataType = gdt };

            gdtVersion.VersionStartDate = DateTime.Now.Date;
            gdtVersion.VersionEndDate = DateTime.Now.Date.AddYears(2);

            ProductGDDefinitionDetail productGDDefinitionDetail = new ProductGDDefinitionDetail();
            productGDDefinitionDetail.ProductGDDefinitionDetailID = 1;
            productGDDefinitionDetail.GenericDataType = gdt;
            productGDDefinitionDetail.GenericDataTypeCode = gdt.Code;

            ProductGDDefinitionHeader productGDDefinitionHeader = new ProductGDDefinitionHeader();
            productGDDefinitionHeader.ProductGDDefinitionHeaderID = 1;
            productGDDefinitionHeader.ProductGDDefinitionDetail.Add(productGDDefinitionDetail);

            ProductClaimDefinition productClaimDefinition = new ProductClaimDefinition();
            productClaimDefinition.InternalProductGDDefinitionHeader = productGDDefinitionHeader;

            productVersion = new ProductVersion { ProductVersionID = 1, Product = new Product() };
            productVersion.Product.Code = "LIABCLM";
            productVersion.ProductClaimDefinition = productClaimDefinition;
            productGDDefinitionHeader.ProductVersion = productVersion;

            var metaEntities = MockRepository.GenerateStub<IMetadataQuery>();
            var list = new List<ProductGDDefinitionDetail>() { productGDDefinitionDetail };
            metaEntities.Stub(m => m.GetAllProductGDDefinitionDetails(1)).Return(list.AsQueryable());
            metaEntities.Stub(m => m.GetGenericDataTypeVersion(1, DateTime.Today.AddDays(-1))).Return(gdtVersion);
            metaEntities.Stub(m => m.GetGenericDataTypeVersionForComponent(74,1,DateTime.Today.AddDays(-1))).Return(gdtVersion);
            return metaEntities;
        }

        private ProductClaimDetail GetProductCalimDetail()
        {
            GenericDataType gdt = new GenericDataType { Code = "UTGDT", CustomCode01 = "BT" };
            GenericDataTypeNames gdtNames = new GenericDataTypeNames { LanguageID = 1, LongDescription = "ClientSpecficTST", GenericDataType = gdt };
            GenericDataTypeVersion gdtVersion = new GenericDataTypeVersion { GenericDataTypeVersionID = 1, GenericDataType = gdt };

            gdtVersion.VersionStartDate = DateTime.Now.Date;
            gdtVersion.VersionEndDate = DateTime.Now.Date.AddYears(2);

            ProductGDDefinitionDetail productGDDefinitionDetail = new ProductGDDefinitionDetail();
            productGDDefinitionDetail.ProductGDDefinitionDetailID = 1;
            productGDDefinitionDetail.GenericDataType = gdt;
            productGDDefinitionDetail.GenericDataTypeCode = gdt.Code;

            ProductGDDefinitionHeader productGDDefinitionHeader = new ProductGDDefinitionHeader();
            productGDDefinitionHeader.ProductGDDefinitionHeaderID = 1;
            productGDDefinitionHeader.ProductGDDefinitionDetail.Add(productGDDefinitionDetail);

            ProductClaimDefinition productClaimDefinition = new ProductClaimDefinition();
            productClaimDefinition.InternalProductGDDefinitionHeader = productGDDefinitionHeader;
            ProductClaimDetail pcd = new ProductClaimDetail();

            pcd.ProductClaimDetailID = 1;
            pcd.ProductVersion = new ProductVersion();
            pcd.ProductVersion.Product = new Product();
            pcd.ProductVersion.ProductClaimDefinition = new ProductClaimDefinition() { ClaimDetailAttachmentChangePreventionThreshold = -1 };
            pcd.ProductVersion.Product.Code = CLAIM_PRODUCT_CODE;
            pcd.ProductVersion.ProductLink.Add(new ProductLink { ProductLinkType = (short)StaticValues.ProductLinkType.ClaimtoPolicy, IsClaimLinkToSectionAllowed = true, IsClaimLinkToSectionDetailAllowed = true, IsClaimLinkToCoverageAllowed = true });
            pcd.ClaimDetailType = new ClaimDetailType();
            pcd.ClaimDetailType.Code = CLAIM_DETAIL_TYPE_CODE;
            productGDDefinitionHeader.InternalProductClaimDetails.Add(pcd);
            return pcd;
        }

        public void TestInitialize(ref ProductClaimDetail pcd, NameType nameType)
        {
            target = new ClientSpecificDataPlugin();

            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();
            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IComponentMetadata>(metadata);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(configurationManager);
            ObjectFactory.Instance = new ObjectFactory(container);

            IInsuranceDirectoryService insuranceDirectoryService = MockRepository.GenerateStub<IInsuranceDirectoryService>();
            container.RegisterInstance<IInsuranceDirectoryService>(insuranceDirectoryService);
            insuranceDirectoryService.Stub(a => a.GetName(-1)).IgnoreArguments().Return(new Name { NameType = (short)nameType });
            insuranceDirectoryService.Stub(a => a.GetName(-2)).IgnoreArguments().Return(new Name { NameType = (short)nameType });
            insuranceDirectoryService.Stub(a => a.GetNames(null)).IgnoreArguments().Return(new List<Name>() { new Name { NameType = (short)nameType }, new Name { NameType = (short)nameType } });

            long productVersionID = 1066;
            ProductVersion productVersion = new ProductVersion();
            productVersion.ProductVersionID = productVersionID;
            if (pcd == null)
            {
                pcd = new ProductClaimDetail();
            }

            this.claimHeader = new ClaimHeader(productVersion.ProductVersionID, DateTime.Now);
            ClaimsTransactionContext context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);
            this.claimHeader.Context = context;
            this.claimHeader.CustomBoolean03 = false;
            ClaimInvolvement claimInvolvement = new ClaimInvolvement();
            claimInvolvement.InternalClaimNameInvolvements.Add(this.component);
            claimInvolvement.ClaimHeader = this.claimHeader;

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
        public void AttachClientSpecific_GenericDataItem_ToClaimHeaderAndClaimDetail_Test()
        {
            this.claimHeader = new ClaimHeader(VALID_CLAIM_PRODUCT_VERSIONID, DateTime.Today.AddDays(-1));
            ClaimInvolvement invo = this.claimHeader.AddNewClaimInvolvement(StaticValues.LinkableComponentType.NameInvolvement);

            ClaimNameInvolvement insured = this.AddInsuredNameInvolvement();
            IAXAClaimsQuery claimQuery = MockRepository.GenerateStub<IAXAClaimsQuery>();
            claimQuery.Stub(q => q.GetPolicyHeaderByHeaderID(2)).Return(this.GetPolicyHeader());

            IInsuranceDirectoryService InsuranceDirectoryService = MockRepository.GenerateStub<IInsuranceDirectoryService>();
            InsuranceDirectoryService.Stub(insu => insu.GetNameUsage(Arg<long>.Is.Anything, Arg<string>.Is.Anything, Arg<DateTime>.Is.Anything)).Return(this.GetNameUsage());

            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            IComponentMetadata compMetadata = MockRepository.GenerateStub<IComponentMetadata>();

            ProductVersion prodVersion = null;

            var metaEntities = this.GetMockData(out prodVersion);

            compMetadata.Stub(m => m.GetDefinitionComponent<ProductVersion>()).Return(prodVersion);
            compMetadata.Stub(m => m.GetDefinitionComponent<ProductClaimDetail>()).Return(this.GetProductCalimDetail());

            UnityContainer container = new UnityContainer();
            container.RegisterInstance<IComponentMetadata>(compMetadata);
            container.RegisterInstance<IMetadataQuery>(metaEntities);
            container.RegisterInstance<IInsuranceDirectoryService>(InsuranceDirectoryService);
            container.RegisterInstance<IAXAClaimsQuery>("ClaimQuery", claimQuery);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterInstance<IConfigurationManager>(configurationManager);

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

            ObjectFactory.Instance = new ObjectFactory(container);

            this.SetContext(this.claimHeader);
            ClientSpecificDataPlugin clp = new ClientSpecificDataPlugin();
            clp.ProcessComponent(insured, ProcessInvocationPoint.Created, 1);
            // clp.PropertyChange(claimHeader,ProcessInvocationPoint.PropertyChange,"PolicyHeaderID",null,newValue,1);
            Assert.IsTrue(this.claimHeader.GenericDataSet != null && this.claimHeader.GenericDataSet.GenericDataItems.Count() == 1);
            Assert.IsTrue(this.claimHeader.GenericDataSet != null && this.claimHeader.GenericDataSet.GenericDataItems.Count() == 1);
        }

        [TestMethod]
        public void CustomReference02FieldVisibility_ForCompanyTest()
        {
            this.component = new ClaimNameInvolvement() { NameID = -1 };
            this.TestInitialize(ref this.pcd, NameType.Company);
            Field field = new Field();
            field.PropertyName = "CustomReference02";
            field.ConfigurableFieldID = 1;
            this.SetContext(this.claimHeader);
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Visible);
        }

        [TestMethod]
        public void CustomReference03FieldVisibility_ForCompanyTest()
        {
            this.component = new ClaimNameInvolvement() { NameID = -1 };
            this.TestInitialize(ref this.pcd, NameType.Company);
            Field field = new Field();
            field.PropertyName = "CustomReference03";
            field.ConfigurableFieldID = 1;
            this.SetContext(this.claimHeader);
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Visible);
        }

        [TestMethod]
        public void CustomReference04FieldVisibility_ForCompanyTest()
        {
            this.component = new ClaimNameInvolvement() { NameID = -1 };
            this.TestInitialize(ref this.pcd, NameType.Company);
            Field field = new Field();
            field.PropertyName = "CustomReference04";
            field.ConfigurableFieldID = 1;
            this.SetContext(this.claimHeader);
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Visible);
        }

        [TestMethod]
        public void CustomReference02FieldVisibility_ForPersonTest()
        {
            this.component = new ClaimNameInvolvement() { NameID = -1, CustomReference02 = "Test Reference02" };
            this.TestInitialize(ref this.pcd, NameType.Person);
            Field field = new Field() { Visible = true };
            field.PropertyName = "CustomReference02";
            field.ConfigurableFieldID = 1;
            Assert.IsTrue(field.Visible);
            this.SetContext(this.claimHeader);
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsFalse(field.Visible);
        }

        [TestMethod]
        public void CustomReference03FieldVisibility_ForPersonTest()
        {
            this.component = new ClaimNameInvolvement() { NameID = -1, CustomReference03 = "Test Reference03" };
            this.TestInitialize(ref this.pcd, NameType.Person);
            Field field = new Field() { Visible = true };
            field.PropertyName = "CustomReference03";
            field.ConfigurableFieldID = 1;
            Assert.IsTrue(field.Visible);
            this.SetContext(this.claimHeader);
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsFalse(field.Visible);
        }

        [TestMethod]
        public void CustomReference04FieldVisibility_ForPersonTest()
        {
            this.component = new ClaimNameInvolvement() { NameID = -1, CustomReference04 = "Test Reference04" };
            this.TestInitialize(ref this.pcd, NameType.Person);
            Field field = new Field() { Visible = true };
            field.PropertyName = "CustomReference04";
            field.ConfigurableFieldID = 1;
            Assert.IsTrue(field.Visible);
            this.SetContext(this.claimHeader);
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsFalse(field.Visible);
        }

        [TestMethod]
        public void UpdateNameInvolvementFieldTest()
        {
            this.component = new ClaimNameInvolvement() { NameID = -1, CustomReference02 = "Test Reference02", CustomReference03 = "Test Reference03", CustomReference04 = "Test Reference04", NameInvolvementType = (short)StaticValues.NameInvolvementType.AdditionalBroker, NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest };
            this.TestInitialize(ref this.pcd, NameType.Person);
            this.SetContext(this.claimHeader);
            target.PropertyChange(this.component, ProcessInvocationPoint.PropertyChange, "NameID", null, (long)-2, -1);
            Assert.IsNull(this.component.CustomReference02);
            Assert.IsNull(this.component.CustomReference03);
            Assert.IsNull(this.component.CustomReference04);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
