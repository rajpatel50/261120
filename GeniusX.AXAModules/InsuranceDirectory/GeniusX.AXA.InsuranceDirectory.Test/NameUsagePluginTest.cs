using System;
using System.Data.Objects;
using GeniusX.AXA.InsuranceDirectory.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Framework;
using Xiap.Framework.Configuration;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.InsuranceDirectory.Test;
using Xiap.Metadata.BusinessComponent;
using Xiap.Testing.Utils;

namespace GeniusX.AXA.InsuranceDirectory.Test
{
    [TestClass]
    public class NameUsagePluginTest : ComponentPluginBaseTest<NameUsagePlugin>
    {
        private NameUsage component;
        private IComponentMetadata metadata;

        [TestInitialize]
        public void TestInitialize()
        {
            target = new NameUsagePlugin();
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

            string[] parameters = { ((int)NameType.Person).ToString() };
            IDMockBusinessTransaction mockBT = GetIDMockBusinessTransaction("CreateName", parameters);

            this.component.Context = mockBT.Context;
        }

        [TestMethod]
        [Ignore]
        public void NewNameUsageNotSameAsExisting()
        {
            NameUsageType nut = new NameUsageType();
            nut.CustomCode01 = "1";

            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nut);


            target = new NameUsagePlugin();
            Name name = new Name();

            NameUsage nu = new NameUsage();
            nu.CustomCode01 = "1";
            nu.Name = name;

            this.component = new NameUsage();
            this.component.CustomCode01 = "0";
            this.component.Name = name;            
            
            this.component.Context = new IDTransactionContext(Guid.NewGuid().ToString(), string.Empty, "CreateName");
            ProcessResultsCollection results =this.target.ProcessComponent(this.component, ProcessInvocationPoint.PreCreateValidation, 1);
            Assert.IsTrue(MessageIdExists(results, "INVALID_NAMEUSAGE"));            
        }

        [TestMethod]
        public void NewNameUsageSameAsExisting()
        {
            NameUsageType nut = new NameUsageType();
            nut.CustomCode01 = "1";

            this.metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nut);


            target = new NameUsagePlugin();
            Name name = new Name();

            NameUsage nu = new NameUsage();
          
            nu.Name = name;

            this.component = new NameUsage();
         
            this.component.Name = name;

            this.component.Context = new IDTransactionContext(Guid.NewGuid().ToString(), string.Empty, "CreateName");
            ProcessResultsCollection results = this.target.ProcessComponent(this.component, ProcessInvocationPoint.PreCreateValidation, 1);
            Assert.IsFalse(MessageIdExists(results, "INVALID_NAMEUSAGE"));           
        }

        private static bool MessageIdExists(ProcessResultsCollection results, string messageId)
        {
            foreach (ProcessResult result in results.Results)
            {
                if (result.MessageId == messageId)
                {
                    return true;
                }
            }

            return false;
        }

        private static IDMockBusinessTransaction GetIDMockBusinessTransaction(string transactionType, string[] parameters)
        {
            IDMockBusinessTransaction mock = new IDMockBusinessTransaction(transactionType, parameters, "InsuranceDirectory");
            return mock;
        }
    }
}
