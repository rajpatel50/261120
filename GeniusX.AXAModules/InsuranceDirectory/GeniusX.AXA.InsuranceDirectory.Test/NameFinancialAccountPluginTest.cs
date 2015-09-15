using System;
using System.Data.Objects;
using GeniusX.AXA.InsuranceDirectory.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Framework;
using Xiap.Framework.Configuration;
using Xiap.Framework.Metadata;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.Metadata.BusinessComponent;
using Xiap.Testing.Utils;

namespace GeniusX.AXA.InsuranceDirectory.Test
{
    [TestClass]
    public class NameFinancialAccountPluginTest : ComponentPluginBaseTest<NameFinancialAccountPlugin>
    {
        private NameFinancialAccount component;
        private IComponentMetadata metadata;

        [TestInitialize]
        public void TestInitialize()
        {
            target = new NameFinancialAccountPlugin();
            this.component = new NameFinancialAccount();

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
        public void TestNameFinancialAccountPluginAutoEntryCreateName()
        {
            Field field = this.GetCustomReference04(IDConstants.NAME_CONTROLLED_OUTSIDE_GENIUSX, "CreateName");
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsFalse(field.Readonly);
        }

        [TestMethod]
        public void TestNameFinancialAccountPluginManualEntryCreateName()
        {
            Field field = this.GetCustomReference04(IDConstants.NAME_CONTROLLED_IN_GENIUSX, "CreateName");
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Readonly);
        }

        [TestMethod]
        public void TestNameFinancialAccountPluginAutoEntryDisplayName()
        {
            Field field = this.GetCustomReference04(IDConstants.NAME_CONTROLLED_OUTSIDE_GENIUSX, "DisplayName");
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Readonly);
        }

        [TestMethod]
        public void TestNameFinancialAccountPluginManualEntryDisplayName()
        {
            Field field = this.GetCustomReference04(IDConstants.NAME_CONTROLLED_IN_GENIUSX, "DisplayName");
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Readonly);
        }

        private Field GetCustomReference04(string fieldControlFlag, string TransactionType)
        {
            Name name = new Name(NameType.Person);
            NameUsage nameusage = new NameUsage("BAK");

            name.NameUsage.Add(nameusage);

            NameFinancialAccount nfa = name.AddNewNameFinancialAccount("BBB");

            NameUsageType nut = new NameUsageType();
            nut.CustomCode01 = fieldControlFlag;
            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nut);

            name.NameUsage.Add(nameusage);

            this.component = nfa;
            this.component.Context = new IDTransactionContext(Guid.NewGuid().ToString(), string.Empty, TransactionType);

            if (TransactionType == "DisplayName")
            {
                this.component.Context.SetUsage(BusinessProcessUsage.DisplayOnly);
            }
            else
            {
                this.component.Context.SetUsage(BusinessProcessUsage.Updateable);
            }

            Field field = new Field() { IsInUse = true, Visible = true };
            field.PropertyName = "CustomReference04";
            return field;
        }
    }
}
