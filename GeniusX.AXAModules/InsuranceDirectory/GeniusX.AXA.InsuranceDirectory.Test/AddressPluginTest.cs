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
    public class AddressPluginTest : ComponentPluginBaseTest<AddressPlugin>
    {
        private Address component;
        private IComponentMetadata metadata;

        [TestInitialize]
        public void TestInitialize()
        {
            target = new AddressPlugin();
            this.component = new Address();

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
        public void TestAddressPluginAutoEntryCreateName()
        {
            Field field = this.GetCustomReference04(IDConstants.NAME_CONTROLLED_OUTSIDE_GENIUSX, "CreateName");
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsFalse(field.Readonly);
        }

        [TestMethod]
        public void TestAddressPluginManualEntryCreateName()
        {
            Field field = this.GetCustomReference04(IDConstants.NAME_CONTROLLED_IN_GENIUSX, "CreateName");
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Readonly);
        }

        [TestMethod]
        public void TestAddressPluginAutoEntryDisplayName()
        {
            Field field = this.GetCustomReference04(IDConstants.NAME_CONTROLLED_OUTSIDE_GENIUSX, "DisplayName");
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Readonly);
        }

        [TestMethod]
        public void TestAddressPluginManualEntryDisplayName()
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

            NameToAddress nta = new NameToAddress();

            Address address = new Address();
            nta.Address = address;

            name.NameToAddress.Add(nta);

            NameUsageType nut = new NameUsageType();
            nut.CustomCode01 = fieldControlFlag;
            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nut);

            name.NameUsage.Add(nameusage);

            this.component = address;
            this.component.Context = new IDTransactionContext(Guid.NewGuid().ToString(), string.Empty, TransactionType);
            this.component.Context.LockedComponents.Add(address);
 
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
