using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
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
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.Framework.Logging;
using Xiap.Testing.Utils.Mocks;
using Xiap.Framework.Common.Metadata;
using Xiap.Framework.Caching;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ClaimNameInvolvementPluginTest : ComponentPluginBaseTest<ClaimNameInvolvementPlugin>
    {
        private ClaimNameInvolvement component;
        private ClaimHeader headerComponent;
        private ClaimDetailToClaimInvolvementLink linkComponent;
        private ProductLinkableComponent productLinkableComponent;
        private ProductClaimDetail pcd = null;
        private ProductXML.ProductClaimDetail productClaimDetail;

        public void TestInitialize(ProductLinkableComponent productLinkableComponent, ref ProductXML.ProductClaimDetail productClaimDetail)
        {
            target = new ClaimNameInvolvementPlugin();

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
            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            var eventTypeVersion = MockRepository.GenerateStub<IEventTypeVersionQuery>();
            eventTypeVersion.Stub(a => a.GetEventTypeVersionFromEventTypeCode("SYSUPD", DateTime.MinValue)).IgnoreArguments().Return(new Xiap.Metadata.Data.XML.EventTypeVersion.EventTypeVersion());
            container.RegisterInstance<IEventTypeVersionQuery>(eventTypeVersion);
            ObjectFactory.Instance = new ObjectFactory(container);
            
            long productVersionID = 1066;
            string claimDetailTypeCode = "LIA";

            ProductVersion productVersion = new ProductVersion();
            productVersion.ProductVersionID = productVersionID;
            productVersion.ProductLinkableComponent.Add(productLinkableComponent);
            productVersion.Product = new Product() { Code = "CGBIPC" };
            metadata.Stub(m => m.GetDefinitionComponent<ProductVersion>()).Return(productVersion);

            if (this.pcd == null)
            {
                this.pcd = new ProductClaimDetail();
            }

            this.pcd.ProductClaimDetailID = 1;
            ProductClaimDetailToComponentLink productClaimDetailToComponentLink = new ProductClaimDetailToComponentLink();
            productClaimDetailToComponentLink.InternalProductLinkableComponent = productLinkableComponent;
            this.pcd.ProductClaimDetailToComponentLink.Add(productClaimDetailToComponentLink);

            metadata.Stub(m => m.GetDefinitionComponent<ProductClaimDetail>()).Return(this.pcd);
            metadata.Stub(m => m.GetDefinitionComponent<ProductVersion>()).Return(productVersion);
            metadataMock.Stub(m => m.GetProductClaimDetail(productVersionID, claimDetailTypeCode)).Return(this.pcd);

            if (productClaimDetail == null)
            {
                productClaimDetail = new ProductXML.ProductClaimDetail();
            }

            productClaimDetail = new ProductXML.ProductClaimDetail { ProductClaimDetailID = 1 };
            productClaimDetail.ProductClaimDetailToComponentLinks = new ProductXML.ProductClaimDetailToComponentLink[1]
                                                                    {
                                                                        new ProductXML.ProductClaimDetailToComponentLink() {ProductLinkableComponentID = 1 }
                                                                    };

            List<ProductXML.ProductClaimDetail> productClaimDetails = new List<ProductXML.ProductClaimDetail> { productClaimDetail };
            IProductClaimDetailQuery claimDetailQuery = MockRepository.GenerateStub<IProductClaimDetailQuery>();
            container.RegisterInstance<IProductClaimDetailQuery>(claimDetailQuery);
            claimDetailQuery.Stub(x => x.GetProductClaimDetails(Arg<long>.Is.Anything)).Return(productClaimDetails);
            claimDetailQuery.Stub(x => x.GetProductClaimDetail(Arg<long>.Is.Anything)).Return(productClaimDetails.First());
            claimDetailQuery.Stub(x => x.GetProductClaimDetail(Arg<long>.Is.Anything, Arg<string>.Is.Equal(claimDetailTypeCode))).Return(productClaimDetails.First());

            DirtyProperty dirtyProperty = new DirtyProperty(10, 20);
            this.component.DirtyPropertyList.Add("NameID", dirtyProperty);

            ProductXML.ProductEvent prodEvent = new ProductXML.ProductEvent();
            prodEvent.ProductEventID = 30;
            prodEvent.EventTypeCode = "SYSUPD";
            List<ProductXML.ProductEvent> prodEventList = new List<ProductXML.ProductEvent>();
            prodEventList.Add(prodEvent);

            IProductEventQuery eventQuery = MockRepository.GenerateStub<IProductEventQuery>();
            eventQuery.Stub(m => m.GetProductEvents(Arg<long>.Is.Anything)).Return(prodEventList.AsQueryable());
            eventQuery.Stub(m => m.GetProductEvent(Arg<long>.Is.Anything)).Return(prodEvent);
            container.RegisterInstance<IProductEventQuery>(eventQuery);

            EventTypeData eventcodedata = MockRepository.GenerateStub<EventTypeData>();
            eventcodedata.Code = "SYSUPD";
            eventcodedata.SetCode = 32;

            List<EventTypeData> eventcomponent = new List<EventTypeData>();
            eventcomponent.Add(eventcodedata);
            metadataMock.Stub(e => e.QueryValueSetValues(Arg<SystemValueSetCodeEnum>.Is.Equal(SystemValueSetCodeEnum.EventType))).Return(eventcomponent.AsQueryable());
            
            ProductEvent productEvent = new ProductEvent();
            productEvent.ProductVersion = productVersion;
            
            productEvent.EventType = new EventType();
            productEvent.EventType.IsStatusChangeEvent = false;
            productEvent.EventType.IsSystemCreationAllowed = false;
            productEvent.EventType.IsManualCreationAllowed = true;
            productEvent.EventType.IsManualDeletionAllowed = true;
            productEvent.EventType.Code = "SYSUPD";
            productEvent.EventTypeCode = "SYSUPD";
            productEvent.ProductEventID = 30;

            EventTypeVersion etv = new EventTypeVersion();
            etv.EventTypeVersionID = 30;

            productEvent.EventType.EventTypeVersion.Add(etv);

            metadata.Stub(m => m.GetDefinitionComponent<EventType>()).Return(productEvent.EventType);

            this.linkComponent = new ClaimDetailToClaimInvolvementLink();

            this.headerComponent = new ClaimHeader(productVersionID, DateTime.Now);
            ClaimsTransactionContext context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);
            this.headerComponent.Context = context;
            this.headerComponent.CustomBoolean03 = false;
            
            ClaimDetail claimDetail1 = new ClaimDetail(productVersionID, claimDetailTypeCode);

            ClaimInvolvement claimInvolvement = new ClaimInvolvement();

            claimDetail1.InternalClaimDetailToClaimInvolvementLinks = new EntityCollection<ClaimDetailToClaimInvolvementLink>();

            claimInvolvement.InternalClaimNameInvolvements = new EntityCollection<ClaimNameInvolvement>();
            this.component.CustomNumeric10 = 123;
            this.component.CustomDate01 = DateTime.Now;
            this.component.CustomBoolean03 = false;

            claimInvolvement.InternalClaimNameInvolvements.Add(this.component);

            this.linkComponent.ClaimInvolvement = claimInvolvement;

            this.linkComponent.ClaimInvolvement.Context = context;
            claimDetail1.Context = context;

            this.headerComponent.InternalClaimDetails.Add(claimDetail1);
            claimInvolvement.ClaimHeader = this.headerComponent;

            claimInvolvement.ProductLinkableComponentID = productLinkableComponent.ProductLinkableComponentID;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.productLinkableComponent = new ProductLinkableComponent();
        }

        [TestMethod]
        public void ClaimHandlerChangeEventTest()
        {
            this.component = new ClaimNameInvolvement();
            this.component.NameInvolvementType = (short)StaticValues.NameInvolvementType.MainClaimHandler;
            this.TestInitialize(this.productLinkableComponent, ref this.productClaimDetail);

            ProcessInvocationPoint point = ProcessInvocationPoint.ComponentChange;
            target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(1, (this.component.Parent.Parent as ClaimHeader).ClaimEvents.Count);
        }

        [TestMethod]
        public void AddClaimDetailIfNIIsClaimaintTest()
        {
            this.productLinkableComponent.ProductLinkableComponentID = 1;

            this.component = new ClaimNameInvolvement() { NameInvolvementType = (short)StaticValues.NameInvolvementType.AdditionalClaimant, NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest };
            this.TestInitialize(this.productLinkableComponent, ref this.productClaimDetail);

            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            Assert.AreEqual(1, this.component.ClaimInvolvement.ClaimDetailToClaimInvolvementLinks.Count);
            target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(2, this.component.ClaimInvolvement.ClaimDetailToClaimInvolvementLinks.Count);
            Assert.IsTrue(this.component.ClaimInvolvement.ClaimDetailToClaimInvolvementLinks[1].ClaimDetail != null);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
