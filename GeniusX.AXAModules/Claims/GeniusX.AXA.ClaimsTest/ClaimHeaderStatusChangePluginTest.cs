using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Caching;
using Xiap.Framework.Configuration;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Testing.Utils;
using Xiap.Testing.Utils.Mocks;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ClaimHeaderStatusChangePluginTest : ComponentPluginBaseTest<ClaimHeaderStatusChangePlugin>
    {
        private ClaimLitigation litigationComponent;
        private ClaimHeader component;

        public void TestInitialize(ProductClaimLitigation productClaimLitigation)
        {
            target = new ClaimHeaderStatusChangePlugin();


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

            DirtyProperty dtp = new DirtyProperty(10, 20);
            this.component.DirtyPropertyList.Add(ClaimConstants.CLAIM_HEADERSTATUS_CODE, dtp);

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

            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            container.RegisterInstance<ILoggingWrapper>(new MockLoggingService());

            this.litigationComponent = new ClaimLitigation(1, litigationInvolvement);
            this.litigationComponent.InternalClaimInvolvement = litigationInvolvement;
            this.litigationComponent.InternalClaimInvolvement.InternalClaimHeader = this.component;
            this.litigationComponent.InternalClaimInvolvement.ClaimHeader.ClaimProductVersionID = 1;
            this.litigationComponent.InternalClaimInvolvement.ClaimHeader.ClaimHeaderStatusCode = "CON";
            this.litigationComponent.InternalClaimInvolvement.ClaimHeader.InternalClaimInvolvements.Add(new ClaimInvolvement() { ClaimInvolvementType = (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.Litigation });
            this.litigationComponent.InternalClaimInvolvement.ClaimHeader.ClaimInvolvements[1].InternalClaimLitigations = new System.Data.Objects.DataClasses.EntityCollection<ClaimLitigation>();
            this.litigationComponent.InternalClaimInvolvement.ClaimHeader.ClaimInvolvements[1].InternalClaimLitigations.Add(new ClaimLitigation() { LitigationReference = "L0001", ProductClaimLitigationID = 1 });
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.component = new ClaimHeader();
            this.TestInitialize(null);
        }

        [TestMethod]
        public void ValidateClaimHeaderStatusCodeTest()
        {
            ProcessInvocationPoint point = ProcessInvocationPoint.Validation;
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
