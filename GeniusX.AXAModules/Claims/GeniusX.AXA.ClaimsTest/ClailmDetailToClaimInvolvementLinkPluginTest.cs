using System;
using System.Collections.Generic;
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
    public class ClailmDetailToClaimInvolvementLinkPluginTest : ComponentPluginBaseTest<ClaimDetailToClaimInvolvementLinkPlugin>
    {
        private ClaimInvolvement component;
        private ClaimHeader headerComponent;
        private ClaimDetailToClaimInvolvementLink linkComponent;

        public void TestInitialize(ProductLinkableComponent productLinkableComponent, ref ProductClaimDetail pcd)
        {
            target = new ClaimDetailToClaimInvolvementLinkPlugin();

            this.component = new ClaimInvolvement();

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
            InsuranceDirectoryEntities Metadata = MockRepository.GenerateStub<InsuranceDirectoryEntities>();
            container.RegisterInstance<InsuranceDirectoryEntities>("InsuranceDirectoryEntities", Metadata);

            container.RegisterType<IInsuranceDirectoryQuery, InsuranceDirectoryEntities>()
                .Configure<InjectedMembers>()
                .ConfigureInjectionFor<InsuranceDirectoryEntities>(new InjectionConstructor());

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

            long productVersionID = 1066;
            string claimDetailTypeCode = "AD";

            ProductVersion productVersion = new ProductVersion();
            productVersion.ProductVersionID = productVersionID;
            productVersion.ProductLinkableComponent.Add(productLinkableComponent);

            if (pcd == null)
            {
                pcd = new ProductClaimDetail();
            }

            pcd.ProductClaimDetailID = 1;
            ProductClaimDetailToComponentLink productClaimDetailToComponentLink = new ProductClaimDetailToComponentLink();
            productClaimDetailToComponentLink.InternalProductLinkableComponent = productLinkableComponent;
            pcd.ProductClaimDetailToComponentLink.Add(productClaimDetailToComponentLink);

            metadata.Stub(m => m.GetDefinitionComponent<ProductClaimDetail>()).Return(pcd);
            metadata.Stub(m => m.GetDefinitionComponent<ProductVersion>()).Return(productVersion);
            metadataMock.Stub(m => m.GetProductClaimDetail(productVersionID, claimDetailTypeCode)).Return(pcd);

            this.linkComponent = new ClaimDetailToClaimInvolvementLink();
            this.headerComponent = new ClaimHeader(productVersion.ProductVersionID, DateTime.Now);
            ClaimsTransactionContext context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);
            this.headerComponent.Context = context;
            ClaimDetail claimDetail = new ClaimDetail(productVersionID, claimDetailTypeCode);

            IInsuranceDirectoryQuery insuranceQuery = MockRepository.GenerateStub<IInsuranceDirectoryQuery>();
            Name nameSample1 = new Name();
            nameSample1.NameType = (short)StaticValues.NameType.Person;
            nameSample1.NameID = -1;
            nameSample1.PersonDetailVersion = new EntityCollection<PersonDetailVersion>();
            nameSample1.PersonDetailVersion.Add(new PersonDetailVersion() { ListName = "IDS Test" });
            insuranceQuery.Stub(x => x.GetName(Arg<long>.Is.Anything)).Return(nameSample1);
            container.RegisterInstance<IInsuranceDirectoryQuery>(insuranceQuery);


            ClaimNameInvolvement claimNI = new ClaimNameInvolvement() { NameInvolvementType = (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant, NameUsageTypeCode = "CLMT", NameID = -1, NameInvolvementMaintenanceStatus = (short?)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest };
     
            claimDetail.InternalClaimDetailToClaimInvolvementLinks = new EntityCollection<ClaimDetailToClaimInvolvementLink>();

            this.component.InternalClaimNameInvolvements = new EntityCollection<ClaimNameInvolvement>();
            this.component.InternalClaimNameInvolvements.Add(claimNI);
            this.component.ClaimInvolvementType = 1;

            this.linkComponent.ClaimInvolvement = this.component;

            this.linkComponent.ClaimInvolvement.Context = context;
            claimDetail.InternalClaimDetailToClaimInvolvementLinks.Add(this.linkComponent);
            claimDetail.Context = context;

            this.headerComponent.InternalClaimDetails.Add(claimDetail);
            this.component.ClaimHeader = this.headerComponent;

            this.component.ProductLinkableComponentID = productLinkableComponent.ProductLinkableComponentID;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ProductLinkableComponent productLinkableComponent = new ProductLinkableComponent();
            ProductClaimDetail pcd = null;
            this.TestInitialize(productLinkableComponent, ref pcd);
        }

        [TestMethod]
        public void SetClaimDetailTitleOnCreationTest()
        {
            target.ProcessComponent(this.linkComponent, ProcessInvocationPoint.Created, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual("AD - IDS Test", this.headerComponent.ClaimDetails.First().ClaimDetailTitle);
        }

        [TestMethod]
        public void RenameClaimDetailTitleOnDeleteTest()
        {
            target.ProcessComponent(this.linkComponent, ProcessInvocationPoint.Created, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual("AD - IDS Test", this.headerComponent.ClaimDetails.First().ClaimDetailTitle);
            target.ProcessComponent(this.linkComponent, ProcessInvocationPoint.Delete, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual("AD", this.headerComponent.ClaimDetails.First().ClaimDetailTitle);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
