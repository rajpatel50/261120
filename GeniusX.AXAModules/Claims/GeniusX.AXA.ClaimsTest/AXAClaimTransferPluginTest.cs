using System;
using System.Data.Objects;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Caching;
using Xiap.Framework.Configuration;
using Xiap.Framework.Entity;
using Xiap.Framework.Logging;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Testing.Utils;
using Xiap.Testing.Utils.Mocks;
using Xiap.Claims.Test;
using Xiap.Framework.Metadata;
using Xiap.Framework.Common.Product;


namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class AXAClaimTransferPluginTest 
    {
        private ClaimHeader claimHeader;
        private ProductClaimDefinition productClaimDefinition;

        [TestInitialize]
        public void Initialize()
        {
            /*
            var container = new UnityContainer();
            var componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            container.RegisterInstance<IComponentMetadata>(componentMetadata);

            var product = new ProductBuilder<ProductVersion>(componentMetadata)
                            .Add(new ProductBuilder<ProductClaimDefinition>(componentMetadata))
                         .Build();

            this.productClaimDefinition = product.ProductClaimDefinition;

            ObjectFactory.Instance = new ObjectFactory(container);
             */
            
            var clmHeader = new BusinessComponentBuilder<ClaimHeader>()
                .SetProperty(a => a.ClaimProductVersionID = 1)
                .SetProperty(a => a.ClaimReference = "AXATEST")
                .Build();

            this.claimHeader = clmHeader; // new ClaimHeader() { ClaimProductVersionID = 0, ClaimReference = "AXATEST" };
        }

        [TestMethod]
        public void ClaimTransferTestStatusChangedTransferSPInvoked()
        {
            this.claimHeader.ClaimHeaderStatusCode = "CRE";
            AXAClaimTransferPlugin transfer = new AXAClaimTransferPlugin();
            IAXAClaimsQuery claimsQuery = MockRepository.GenerateStub<IAXAClaimsQuery>();
            claimsQuery.Stub(m => m.ExecuteClaimTransferControlLogSP("AXATEST"));

            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());

            UnityContainer container = new UnityContainer();

            container.RegisterInstance<IAXAClaimsQuery>(claimsQuery);
            container.RegisterInstance<ICopyValidation>(MockRepository.GenerateStub<Xiap.Framework.Common.CopyProcessPlugin>());
            container.RegisterInstance<IConfigurationManager>(configurationManager);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            container.RegisterInstance<ILoggingWrapper>(new MockLoggingService());

            IMetadataQuery metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            container.RegisterInstance<IMetadataQuery>(metadataMock);

            ObjectFactory.Instance = new ObjectFactory(container);

            EFTransactionContext context = new EFTransactionContext(Guid.NewGuid().ToString(), string.Empty, "AmendClaim");
            string[] parameters = { this.claimHeader.ClaimHeaderID.ToString() };
            Xiap.Testing.Utils.Mocks.MockEFBusinessTransaction mockBT = new Xiap.Testing.Utils.Mocks.MockEFBusinessTransaction(context) { Component = this.claimHeader};
            this.claimHeader.ClaimHeaderStatusCode = this.claimHeader.OriginalTransitionStatus;
            this.claimHeader.ClaimHeaderStatusCode = "COU";
            ProcessResultsCollection results = transfer.ProcessTransaction(mockBT, TransactionInvocationPoint.PostComplete, 1, parameters);

            claimsQuery.AssertWasCalled(x => x.ExecuteClaimTransferControlLogSP("AXATEST"));
        }

        [TestMethod]
        public void ClaimTransferTestStatusNotChangedTransferSPNotInvoked()
        {
            this.claimHeader.ClaimHeaderStatusCode = "COU";
            AXAClaimTransferPlugin transfer = new AXAClaimTransferPlugin();
            IAXAClaimsQuery claimsQuery = MockRepository.GenerateStub<IAXAClaimsQuery>();
            claimsQuery.Stub(m => m.ExecuteClaimTransferControlLogSP("AXATEST"));

            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());

            UnityContainer container = new UnityContainer();

            container.RegisterInstance<IAXAClaimsQuery>(claimsQuery);
            container.RegisterInstance<ICopyValidation>(MockRepository.GenerateStub<Xiap.Framework.Common.CopyProcessPlugin>());
            container.RegisterInstance<IConfigurationManager>(configurationManager);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            container.RegisterInstance<ILoggingWrapper>(new MockLoggingService());

            IMetadataQuery metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            container.RegisterInstance<IMetadataQuery>(metadataMock);

            ObjectFactory.Instance = new ObjectFactory(container);

            EFTransactionContext context = new EFTransactionContext(Guid.NewGuid().ToString(), string.Empty, "AmendClaim");
            string[] parameters = { this.claimHeader.ClaimHeaderID.ToString() };
            Xiap.Testing.Utils.Mocks.MockEFBusinessTransaction mockBT = new Xiap.Testing.Utils.Mocks.MockEFBusinessTransaction(context) { Component = this.claimHeader };
            this.claimHeader.ClaimHeaderStatusCode = this.claimHeader.OriginalTransitionStatus;
            this.claimHeader.ClaimHeaderStatusCode = "COU";
            ProcessResultsCollection results = transfer.ProcessTransaction(mockBT, TransactionInvocationPoint.PostComplete, 1, parameters);

            claimsQuery.AssertWasNotCalled(x => x.ExecuteClaimTransferControlLogSP("AXATEST"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
