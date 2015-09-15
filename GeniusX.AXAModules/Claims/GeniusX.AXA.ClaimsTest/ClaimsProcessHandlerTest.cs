using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Common.Product;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using K2ServiceClient.ProcessManagement;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ClaimsProcessHandlerTest : ComponentPluginBaseTest<ClaimsProcessHandler>
    {
        private const string AXAFileUploadNotificationProcessHandler = "AXAFileUploadNotificationProcessHandler";
        private ClaimEvent component = null;
        private IProcessManagement processManagement;

        [TestInitialize]
        public void Initialise()
        {
            this.component = new ClaimEvent();
            ProductEvent productEvent = new ProductEvent();
            this.component.ProductEventID = 34;
            ClaimHeader claimHeader = new BusinessComponentBuilder<ClaimHeader>()
                                        .SetProperty(a => a.ClaimHeaderID = 1)
                                        .SetProperty(a => a.ClaimReference = "XUK1029214MO")
                                        .SetProperty(a => a.ClaimProductVersionID = 1)
                                        .SetProperty(a => a.ClaimStage = (short)StaticValues.ClaimStage.Claim)
                                        .Add(new BusinessComponentBuilder<ClaimDocument>()
                                        .SetProperty(a => a.DocumentReference = "DGR01")
                                        .SetProperty(a => a.DocumentGroupReference = "DGR01")
                                        .SetProperty(a => a.ProductDocumentID = 1)).Build();


            this.component.InternalClaimHeader = claimHeader;
            this.component.ClaimHeader.InternalClaimDetails.Add(new ClaimDetail() { ClaimDetailReference = "D0001" });

            ClaimInvolvement claimInvolvement = new ClaimInvolvement() { InternalClaimHeader = this.component.InternalClaimHeader, ClaimInvolvementType = (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.NameInvolvement, InternalClaimNameInvolvements = new System.Data.Objects.DataClasses.EntityCollection<ClaimNameInvolvement>() };
            ClaimNameInvolvement nameInvolvement = new ClaimNameInvolvement() { NameInvolvementType = (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MainClaimHandler, NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest, NameID = 1 };
            claimInvolvement.InternalClaimNameInvolvements.Add(nameInvolvement);
            this.component.InternalClaimHeader.InternalClaimInvolvements.Add(claimInvolvement);
            this.component.CustomReference01 = "DGR01";
            this.component.CustomReference02 = "DGR02";

            productEvent.EventType = new EventType();
            productEvent.EventType.IsStatusChangeEvent = false;
            productEvent.EventType.IsSystemCreationAllowed = false;
            productEvent.EventType.IsManualCreationAllowed = true;
            productEvent.EventType.IsManualDeletionAllowed = true;
            productEvent.EventType.Code = "ABCD";
            Product prd = new Product { Code = "AXAPRD" };
            ProductVersion prv = new ProductVersion { ProductVersionID = 1, Product = prd };
            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();
            metadata.Stub(m => m.GetDefinitionComponent<EventType>()).Return(productEvent.EventType);
            metadata.Stub(x => x.GetDefinitionComponent<ProductVersion>()).Return(new ProductVersion { ProductVersionID = 1, Product = new Product() { Code = "AXAPRD" } });

            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IComponentMetadata>(metadata);

            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            var list = new List<ProductEvent>() { productEvent };
            long productEventID = 34;

            metadataMock.Stub(m => m.GetProductEvent(productEventID));

            var listEventType = new List<EventType>() { productEvent.EventType };
            metadataMock.Stub(m => m.GetEventType("ABCD")).Return(listEventType.AsQueryable());
            metadataMock.Stub(m => m.GetUserByNameId(1)).Return(new User() { UserIdentity = "TestUser", UserID = 1 });

            container.RegisterInstance<IMetadataQuery>(metadataMock);
            ObjectFactory.Instance = new ObjectFactory(container);

            IDateAvailability dateAvailability = MockRepository.GenerateStub<IDateAvailability>();
            container.RegisterInstance<IDateAvailability>(dateAvailability);

            this.processManagement = MockRepository.GenerateStub<IProcessManagement>();
            this.processManagement.Stub(a => a.Instantiate(Arg<InstantiateProcessRequest>.Is.Anything)).Return(1);
            container.RegisterInstance<IProcessManagement>(this.processManagement);

            IDecisionTableHelper decisionTableHelper = MockRepository.GenerateStub<IDecisionTableHelper>();
            IDecisionTableComponent decisionTableComponent = MockRepository.GenerateStub<IDecisionTableComponent>();
            decisionTableComponent.Action1 = StaticValues.NameInvolvementType.MainClaimHandler;
            decisionTableComponent.Action2 = "true";
            decisionTableComponent.Action3 = "false";
            List<ICodeValueNamesComponent> component = new List<ICodeValueNamesComponent>();
            metadataMock.Stub(m => m.QueryValueSetNames(SystemValueSetCodeEnum.CoverageTypeCode, 1)).Return(component.AsQueryable());

            decisionTableHelper.Stub(a => a.TryCall(Arg<string>.Is.Anything,
            Arg<DateTime>.Is.Anything,
            Arg<IEnumerable<IBusinessComponent>>.Is.Anything,
            Arg<IDictionary<int, string>>.Is.Anything,
            out Arg<IDecisionTableComponent>.Out(decisionTableComponent).Dummy,
            Arg<object[]>.Is.Anything))
            .Return(true);

            ProductXML.ProductEvent prodEvent = new ProductXML.ProductEvent();
            prodEvent.EventTypeCode = "FILUPNC";
            prodEvent.ProductEventID = 1;

            List<ProductXML.ProductEvent> prodEventList = new List<ProductXML.ProductEvent>() { prodEvent };
            IProductEventQuery eventQuery = MockRepository.GenerateStub<IProductEventQuery>();
            eventQuery.Stub(m => m.GetProductEvents(Arg<long>.Is.Anything)).Return(prodEventList.AsQueryable());
            container.RegisterInstance<IProductEventQuery>(eventQuery);

            container.RegisterInstance<IDecisionTableHelper>(decisionTableHelper);
            container.RegisterInstance<IDecisionTableComponent>(decisionTableComponent);

            this.target = new ClaimsProcessHandler();
        }

        [TestMethod]
        public void ProcessComponent_InstanciateAXAFileUploadNotification_Suceess()
        {
            this.component.CustomCode08 = "1";
            ProcessResultsCollection resultCollection = this.target.ProcessComponent(this.component, ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Alias = AXAFileUploadNotificationProcessHandler});
            this.processManagement.AssertWasCalled(a => a.Instantiate(Arg<InstantiateProcessRequest>.Is.Anything));
        }

        [TestMethod]
        public void ProcessComponent_DontCreatePrcessIfCustomBoolean01false_ProceessNotCreated()
        {
            this.component.CustomCode08 = "0";
            ProcessResultsCollection resultCollection = this.target.ProcessComponent(this.component, ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Alias = AXAFileUploadNotificationProcessHandler });
            this.processManagement.AssertWasNotCalled(a => a.Instantiate(Arg<InstantiateProcessRequest>.Is.Anything));
        }
    }
}

       


           

