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
    [TestClass]
    public class ClaimLitigationPluginTest : ComponentPluginBaseTest<GeniusX.AXA.Claims.BusinessLogic.ClaimLitigationPlugin>
    {
        private ClaimLitigation component;
        
        public void TestInitialize(ProductClaimLitigation productClaimLitigation)
        {
            target = new GeniusX.AXA.Claims.BusinessLogic.ClaimLitigationPlugin();


            if (productClaimLitigation == null)
            {
                productClaimLitigation = new ProductClaimLitigation();
            }

            productClaimLitigation.ProductClaimLitigationID = 1;
            productClaimLitigation.InternalProductVersion = new ProductVersion();
            productClaimLitigation.ProductVersion.Product = new Product();
            productClaimLitigation.ProductVersion.Product.Code = "TST";
            productClaimLitigation.InternalLitigationType = new LitigationType { Code = "LType" };

            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();
            metadata.Stub(m => m.GetDefinitionComponent<ProductClaimLitigation>()).Return(productClaimLitigation);
            metadata.Stub(m => m.GetDefinitionComponent<ProductVersion>()).Return(new ProductVersion { ProductVersionID = 1 });
            metadata.Stub(m => m.DefinitionComponent).Return(new ProductVersion { ProductVersionID = 1 });
            metadata.Stub(m => m.DefinitionComponent).Return(productClaimLitigation);

            
            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IComponentMetadata>(metadata);


            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            var list = new List<short?>() { 1 };
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            metadataMock.Stub(c => c.GetProductClaimLitigation(1, "TST")).Return(productClaimLitigation);
            metadataMock.Stub(c => c.GetProductClaimLitigationCode(1, 2427)).Return("TYPE");
            metadataMock.Stub(c => c.GetProductClaimLitigation(1, "LType")).Return(productClaimLitigation);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(configurationManager);

            ObjectFactory.Instance = new ObjectFactory(container);
            metadataMock.Stub(c => c.GetProductClaimLitigation(1)).Return(productClaimLitigation);
            var litigationInvolvement = new ClaimInvolvement();
            this.component = new ClaimLitigation(1, litigationInvolvement);
            this.component.InternalClaimInvolvement = litigationInvolvement;
            this.component.InternalClaimInvolvement.InternalClaimHeader = new ClaimHeader();
            this.component.InternalClaimInvolvement.ClaimHeader.ClaimHeaderStatusCode = "CON";
            this.component.InternalClaimInvolvement.ClaimHeader.ClaimProductVersionID = 1;
            this.component.InternalClaimInvolvement.ClaimHeader.InternalClaimInvolvements.Add(new ClaimInvolvement() { ClaimInvolvementType = (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.Litigation });
            this.component.InternalClaimInvolvement.ClaimHeader.ClaimInvolvements[1].InternalClaimLitigations = new System.Data.Objects.DataClasses.EntityCollection<ClaimLitigation>();
            this.component.InternalClaimInvolvement.ClaimHeader.ClaimInvolvements[1].InternalClaimLitigations.Add(new ClaimLitigation() { LitigationReference = "L0001", ProductClaimLitigationID = 1 });
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.TestInitialize(null);
        }

        [TestMethod]
        public void ValidateClaimHeaderStatusCodeLitigationTest()
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
