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
using Xiap.Framework.Entity;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.Framework.Common.Product;
using Xiap.Metadata.Data;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;
namespace GeniusX.AXA.Claims.Test
{
    [TestClass]
    public class ClaimEventPluginTest : ComponentPluginBaseTest<ClaimsEventPlugin>
    {
        private ClaimEvent component;
        private ProductEvent productEvent;
        private UnityContainer _container;
        internal static void TestInitialize(ref ClaimEvent component, ref ProductEvent pce,ref UnityContainer _container)
        {
            component = new ClaimEvent();
            component.ProductEventID = 34;

            if (pce == null)
            {
                pce = new ProductEvent();
            }

            component.InternalClaimHeader = new ClaimHeader();
            component.ClaimHeader.InternalClaimDetails.Add(new ClaimDetail() { ClaimDetailReference = "D0001" });
            ClaimInvolvement claimInvolvement = new ClaimInvolvement() { InternalClaimHeader = component.InternalClaimHeader, ClaimInvolvementType = (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.NameInvolvement, InternalClaimNameInvolvements = new System.Data.Objects.DataClasses.EntityCollection<ClaimNameInvolvement>() };
            ClaimNameInvolvement nameInvolvement = new ClaimNameInvolvement() { NameInvolvementType = (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MainClaimHandler, NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest, NameID = 1 };
            claimInvolvement.InternalClaimNameInvolvements.Add(nameInvolvement);
            component.InternalClaimHeader.InternalClaimInvolvements.Add(claimInvolvement);

            pce.ProductVersion = new ProductVersion();
            pce.ProductVersion.Product = new Product();
            pce.ProductVersion.Product.Code = "TST";

            pce.EventType = new EventType();
            pce.EventType.IsStatusChangeEvent = false;
            pce.EventType.IsSystemCreationAllowed = false;
            pce.EventType.IsManualCreationAllowed = true;
            pce.EventType.IsManualDeletionAllowed = true;
            pce.EventType.Code = "ABCD";
            pce.EventTypeReference.SetEntityKey(new System.Data.EntityKey("MetadataEntities.EventType", "Code", "ABCD"));

            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();
            metadata.Stub(m => m.GetDefinitionComponent<EventType>()).Return(pce.EventType);

            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IComponentMetadata>(metadata);

            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            var list = new List<ProductEvent>() { pce };
            long productEventID = 34;

            metadataMock.Stub(m => m.GetProductEvent(productEventID));

            var listEventType = new List<EventType>() { pce.EventType };
            metadataMock.Stub(m => m.GetEventType("ABCD")).Return(listEventType.AsQueryable());

            EventTypeVersion etv = new EventTypeVersion();
            etv.EventTypeVersionID = 34;
            var listTypeVersion = new List<EventTypeVersion>() { etv };
            metadataMock.Stub(m => m.GetEventTypeVersion("ABCD")).Return(listTypeVersion.AsQueryable());

            metadataMock.Stub(m => m.GetUserByNameId(1)).Return(new User() { UserIdentity = "TestUser", UserID = 1 });

            container.RegisterInstance<IMetadataQuery>(metadataMock);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(configurationManager);
            ObjectFactory.Instance = new ObjectFactory(container);

            component.Context = new ClaimsTransactionContext(string.Empty, string.Empty, string.Empty);
            _container = container;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            target = new ClaimsEventPlugin();
            ClaimEventPluginTest.TestInitialize(ref this.component, ref this.productEvent,ref this._container);
        }

        [TestMethod]
        [Ignore]
        public void Processcomponent_Create()
        {
            this.productEvent.EventType.IsStatusChangeEvent = false;
            this.productEvent.EventType.IsSystemCreationAllowed = true;
            this.productEvent.EventType.IsManualCreationAllowed = true;
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsNotNull(this.component.TaskInitialUserID, "Task Initial User Id is not Defaulted");
        }

        [TestMethod]
        public void Validate_TaskUserIDIsNull_PostEvent()
        {   
            ProductXML.ProductEvent prodEvent = new ProductXML.ProductEvent();
            prodEvent.EventTypeCode = "POST";

            List<ProductXML.ProductEvent> prodEventList = new List<ProductXML.ProductEvent>();
            prodEventList.Add(prodEvent);

            IProductEventQuery eventQuery = MockRepository.GenerateStub<IProductEventQuery>();
            eventQuery.Stub(m => m.GetProductEvents(Arg<long>.Is.Anything)).Return(prodEventList.AsQueryable());
            eventQuery.Stub(m => m.GetProductEvent(Arg<long>.Is.Anything)).Return(prodEvent);
            this._container.RegisterInstance<IProductEventQuery>(eventQuery);

            this.component.CustomCode02 = "REC";
            ProcessInvocationPoint point = ProcessInvocationPoint.Virtual;
            target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsNull(this.component.TaskInitialUserID, "Task Initial User is null");
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
