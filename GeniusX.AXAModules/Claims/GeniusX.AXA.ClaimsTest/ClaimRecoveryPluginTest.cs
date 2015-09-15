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
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Testing.Utils;

namespace GeniusX.AXA.ClaimsTest
{
    //// <summary>
    //// Summary description for ValidateClaimHeaderStatusCodeRecoveryPluginTest
    //// </summary>
    [TestClass]
    public class ClaimRecoveryPluginTest : ComponentPluginBaseTest<GeniusX.AXA.Claims.BusinessLogic.ClaimRecoveryPlugin>
    {
        private ClaimRecovery component;
        private TestContext testContextInstance;

        public ClaimRecoveryPluginTest() 
        {
        }
                
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return this.testContextInstance;
            }

            set
            {
                this.testContextInstance = value;
            }
        }

        internal static void TestInitialize(ref ProductClaimRecovery pcr, ref ClaimRecovery component)
        {
            ////component.RecoveryType = "TST";
            if (pcr == null)
            {
                pcr = new ProductClaimRecovery();
            }

            pcr.ProductClaimRecoveryID = 1;

            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();
            metadata.Stub(m => m.GetDefinitionComponent<ProductClaimRecovery>()).Return(pcr);
            metadata.Stub(m => m.DefinitionComponent).Return(pcr);

            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IComponentMetadata>(metadata);

            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            var list = new List<short?>() { 1 };
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            metadataMock.Stub(c => c.GetProductClaimRecovery(1, "TST")).Return(pcr);
            metadataMock.Stub(c => c.GetProductClaimRecoveryTypeCode(1)).Return("TYPE");

            container.RegisterInstance<IMetadataQuery>(metadataMock);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(configurationManager);

            ObjectFactory.Instance = new ObjectFactory(container);

            var recParent = new ClaimInvolvement();
            component = new ClaimRecovery(1, "TST", recParent);
            component.InternalClaimInvolvement = recParent;
            component.InternalClaimInvolvement.ClaimHeader = new ClaimHeader();
            component.InternalClaimInvolvement.ClaimHeader.ClaimHeaderStatusCode = "CON";
            component.InternalClaimInvolvement.ClaimHeader.ClaimProductVersionID = 1;
            component.InternalClaimInvolvement.ClaimHeader.InternalClaimInvolvements.Add(new ClaimInvolvement() { ClaimInvolvementType = (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.Recovery });
            component.InternalClaimInvolvement.ClaimHeader.ClaimInvolvements[1].InternalClaimRecoveries = new System.Data.Objects.DataClasses.EntityCollection<ClaimRecovery>();
            component.InternalClaimInvolvement.ClaimHeader.ClaimInvolvements[1].InternalClaimRecoveries.Add(new ClaimRecovery() { RecoveryReference = "C0001" });

            pcr.InternalProductVersion = new ProductVersion();
            pcr.InternalProductVersion.Product = new Product();
            pcr.InternalProductVersion.Product.Code = "TST";
            pcr.InternalRecoveryType = new RecoveryType();
            pcr.InternalRecoveryType.Code = "TYPE";
        }


        [TestInitialize]
        public void TestInitialize()
        {
            target = new GeniusX.AXA.Claims.BusinessLogic.ClaimRecoveryPlugin();
            ProductClaimRecovery pcr = null;
            ClaimRecoveryPluginTest.TestInitialize(ref pcr, ref this.component);
        }

        [TestMethod]
        public void ValidateClaimHeaderStatusCodeRecoveryTest()
        {
            ProcessInvocationPoint point = ProcessInvocationPoint.PreCreateValidation;
            ProcessResultsCollection result = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = string.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(ClaimConstants.LITIGATION_OR_RECOVERY_NOT_ALLOWED, result.Results.FirstOrDefault().MessageTitle);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
