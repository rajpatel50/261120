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
using System;

namespace GeniusX.AXA.InsuranceDirectory.Test
{
    [TestClass]
    public class CreateNamePluginTest : ComponentPluginBaseTest<CreateNamePlugin>
    {
        private Name component;
        private IComponentMetadata metadata;

        [TestInitialize]
        public void TestInitialize()
        {
            target = new CreateNamePlugin();
            this.component = new Name();
            // this.component.Name = new Name();

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
        public void TestCreateNamePluginManualAutoEntry()
        {
            Name name = new Name(NameType.Person);
            NameUsage nameusage=new NameUsage("BAK");
            
            NameUsageType nut = new NameUsageType();
            nut.CustomCode01 = "0";
            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nut);
            name.Context = new IDTransactionContext(Guid.NewGuid().ToString(), string.Empty, "CreateName");
            name.Context.SetUsage(BusinessProcessUsage.Updateable);
            name.NameUsage.Add(nameusage);
            this.component = name;            
            Field field = new Field();
            field.PropertyName = "NameReference";
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsFalse(field.Readonly);
        }

        [TestMethod]
        public void TestCreateNamePluginManualEntry()
        {
            Name name = new Name(NameType.Person);
            NameUsage nameusage = new NameUsage("BAK");

            NameUsageType nut = new NameUsageType();
            nut.CustomCode01 = "1";
            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nut);

            name.NameUsage.Add(nameusage);
            this.component = name;            
            Field field = new Field();
            field.PropertyName = "NameReference";
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Readonly);
        }


        private static IDMockBusinessTransaction GetIDMockBusinessTransaction(string transactionType, string[] parameters)
        {
            IDMockBusinessTransaction mock = new IDMockBusinessTransaction(transactionType, parameters, "InsuranceDirectory");
            return mock;
        }
    }
}
