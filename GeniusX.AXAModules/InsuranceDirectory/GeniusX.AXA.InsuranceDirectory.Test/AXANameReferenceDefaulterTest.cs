using System.Data.Objects;
using GeniusX.AXA.InsuranceDirectory.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Framework;
using Xiap.Framework.Configuration;
using Xiap.Framework.Metadata;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.InsuranceDirectory.Test;
using Xiap.Metadata.BusinessComponent;
using Xiap.Testing.Utils;

namespace GeniusX.AXA.InsuranceDirectory.Test
{
    [TestClass]
    public class AXANameReferenceDefaulterTest : ComponentPluginBaseTest<AXANameReferenceDefaulterPlugin>
    {
        private NameUsage component;
        private IComponentMetadata metadata;

        [TestInitialize]
        public void TestInitialize()
        {
            target = new AXANameReferenceDefaulterPlugin();
            this.component = new NameUsage();
            this.component.Name = new Name();

            this.metadata = MockRepository.GenerateStub<IComponentMetadata>();
            UnityContainer container = new UnityContainer();

            container.RegisterInstance<IComponentMetadata>(this.metadata);
            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(configurationManager);

            ObjectFactory.Instance = new ObjectFactory(container);
        }

        [TestMethod]
        public void GeniusXNameUsage()
        {
            string[] parameters = { ((int)NameType.Person).ToString() };
            IDMockBusinessTransaction mockBT = GetIDMockBusinessTransaction("CreateName", parameters);
            this.component.Context = mockBT.Context;
            this.LockTransactionID.Add(mockBT.Context.TransactionId);
            NameUsageType nut = new NameUsageType();
            nut.CustomCode01 = "1";

            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nut);

            this.target.ProcessComponent(this.component, ProcessInvocationPoint.Created, 1);
            Assert.IsNotNull(this.component.Name.NameReference);
            Assert.AreEqual(8, this.component.Name.NameReference.Length);
        }

        [TestMethod]
        public void GeniusNameUsage()
        {
            string[] parameters = { ((int)NameType.Person).ToString() };
            IDMockBusinessTransaction mockBT = GetIDMockBusinessTransaction("CreateName", parameters);
            this.component.Context = mockBT.Context;
            this.LockTransactionID.Add(mockBT.Context.TransactionId);
            NameUsageType nut = new NameUsageType();
            nut.CustomCode01 = "0";

            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nut);

            this.target.ProcessComponent(this.component, ProcessInvocationPoint.Created, 1);
            Assert.IsNull(this.component.Name.NameReference);
        }

        [TestMethod]
        public void GeniusCopyName_CustomeCode01AS1_DefaultNameRefGeneration()
        {
            string sourceRef = "TestSourceRef";
            string[] parameters = { "2", sourceRef, string.Empty, "2", "1", null, "1", "1" };
            IDMockBusinessTransaction mockBT = GetIDMockBusinessTransaction("CopyName", parameters);
            this.component.Context = mockBT.Context;
            this.LockTransactionID.Add(mockBT.Context.TransactionId);
            NameUsageType nameUsageType = new NameUsageType();
            nameUsageType.CustomCode01 = "1";

            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nameUsageType);

            this.component.Context.CopyDictionary.Add(this.component.Name.DataId, this.component.Name);

            this.target.ProcessComponent(this.component, ProcessInvocationPoint.Copy, 1);
            Assert.IsNotNull(this.component.Name.NameReference);
            Assert.AreNotEqual(sourceRef, this.component.Name.NameReference);
        }

        [TestMethod]
        public void GeniusCopyName_CustomeCode01AS0_DefaultNameRefGeneration()
        {
            string sourceRef = "TestSourceRef";
            string[] parameters = { "2", sourceRef, string.Empty, "2", "1", null, "1", "1" };
            IDMockBusinessTransaction mockBT = GetIDMockBusinessTransaction("CopyName", parameters);
            this.component.Context = mockBT.Context;
            this.LockTransactionID.Add(mockBT.Context.TransactionId);
            NameUsageType nameUsageType = new NameUsageType();
            nameUsageType.CustomCode01 = "0";

            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nameUsageType);

            this.component.Context.CopyDictionary.Add(this.component.Name.DataId, this.component.Name);

            this.target.ProcessComponent(this.component, ProcessInvocationPoint.Copy, 1);
            Assert.IsNull(this.component.Name.NameReference);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }

        private static IDMockBusinessTransaction GetIDMockBusinessTransaction(string transactionType, string[] parameters)
        {
            IDMockBusinessTransaction mock = new IDMockBusinessTransaction(transactionType, parameters, "InsuranceDirectory");
            return mock;
        }
    }
}
