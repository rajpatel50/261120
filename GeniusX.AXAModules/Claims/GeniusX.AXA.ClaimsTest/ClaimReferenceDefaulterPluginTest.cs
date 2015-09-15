using System;
using System.Data.Objects;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Configuration;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework.Security;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;


namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ClaimReferenceDefaulterPluginTest : ComponentPluginBaseTest<ClaimReferenceDefaulterPlugin>
    {
        private ClaimHeader claimHeader;

        [TestInitialize]
        public void TestInitialize()
        {
            const string motorClaimProduct = "CGBIMO";
            target = new ClaimReferenceDefaulterPlugin();
            
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

            IDecisionTableHelper decisionTableHelper = MockRepository.GenerateStub<IDecisionTableHelper>();
            IDecisionTableComponent decisionTableComponent = MockRepository.GenerateStub<IDecisionTableComponent>();
            decisionTableComponent.Action1 = "XUK";
            
            string customCode01 =  XiapSecurity.GetUser().CustomCode01;
            decisionTableHelper.Stub(s => s.Call(ClaimConstants.CLAIMREFERENCEPREFIX_DECISIONTABLE, DateTime.Today, customCode01)).Return(decisionTableComponent);
            
            container.RegisterInstance<IDecisionTableHelper>(decisionTableHelper);

            ClaimsEntities claimEntity = MockRepository.GenerateStub<ClaimsEntities>();
            container.RegisterInstance<ClaimsEntities>("ClaimsEntities", claimEntity);

            long productVersionID = 1066;
            
            ProductVersion productVersion = new ProductVersion();
            productVersion.ProductVersionID = productVersionID;

            productVersion.Product = new Product();
            productVersion.Product.ClaimStage = (short)StaticValues.ClaimStage.Claim;
            productVersion.Product.Code = motorClaimProduct;

            metadata.Stub(m => m.GetDefinitionComponent<ProductVersion>()).Return(productVersion);

            this.claimHeader = new ClaimHeader(productVersion.ProductVersionID, DateTime.Now);
            this.claimHeader.ClaimHeaderAnalysisCode01 = "MOT";

            ClaimsTransactionContext context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);
            this.claimHeader.Context = context;
        }

        [TestMethod]
        public void ClaimReferenceDefaulterAtCreateClaimTest()
        {
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            Assert.IsNull(this.claimHeader.ClaimReference);
            ProcessResultsCollection result = target.ProcessComponent(this.claimHeader, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsNotNull(this.claimHeader.ClaimReference);
            Assert.IsTrue(this.claimHeader.ClaimReference.EndsWith("MO"));
        }

        [TestMethod]
        public void ClaimReferenceDefaulterAtCopyClaimTest()
        {
            ProcessInvocationPoint point = ProcessInvocationPoint.Copy;
            ClaimHeader clmhdr = new ClaimHeader();
            clmhdr.ClaimHeaderAnalysisCode01 = "MOT";
            ClaimsTransactionContext context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);
            clmhdr.Context = context;
            this.claimHeader.Context.CopyDictionary.Add(this.claimHeader.DataId, clmhdr);
            Assert.IsNull(clmhdr.ClaimReference);
            ProcessResultsCollection result = target.ProcessComponent(this.claimHeader, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsNotNull(clmhdr.ClaimReference);
            Assert.IsTrue(clmhdr.ClaimReference.EndsWith("MO"));
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
