using System;
using GeniusX.AXA.InsuranceDirectory.BusinessLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xiap.Framework;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.Testing.Utils;
using Xiap.Framework.Metadata;
using Rhino.Mocks;
using Microsoft.Practices.Unity;
using Xiap.Metadata.BusinessComponent;
using System.Data.Objects;
using Xiap.Framework.Messages;
using Xiap.Framework.Caching;

namespace GeniusX.AXA.InsuranceDirectory.Test
{
    [TestClass]
    public class PersonDetailPluginTest : ComponentPluginBaseTest<PersonDetailPlugin>
    {
        private PersonDetailVersion component;
        
        [TestInitialize]
        public void TestInitialize()
        {
            target = new PersonDetailPlugin();
            this.component = new PersonDetailVersion(1L);
            this.component.Surname = "s";
            this.component.Forename = "f";
            this.component.VersionStartDate = XiapConstants.StartOfTime;
            this.component.VersionEndDate = XiapConstants.EndOfTime;
            this.component.IsLatestVersion = true;
            IComponentMetadata componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            IUnityContainer container = new UnityContainer();
            var metadataEntities = new MetadataEntities();
            container.RegisterInstance<MetadataEntities>("MetadataEntities", metadataEntities);
            componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            IMessageService messageservie = new MessageService();
            container.RegisterInstance<IMessageService>(messageservie);
            container.RegisterInstance<IComponentMetadata>(componentMetadata);
            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            ILookupDefinitionCache lookupDefinitionCache = MockRepository.GenerateStub<ILookupDefinitionCache>();
            ILookupDefinition lookupDefinition = MockRepository.GenerateStub<ILookupDefinition>();
            lookupDefinitionCache.Stub(a => a.GetLookupDefinition(string.Empty, 0)).IgnoreArguments().Return(lookupDefinition);
            componentMetadata.Stub(a => a.GetField("TitleCode")).Return(new Field { ConfigurableFieldID = 1, PropertyName = "TitleCode", LookupDefinitionKey = new BusinessComponentKey("StaticValueSet") { new BusinessComponentKeyMember("SetCode", 100083) } });
            componentMetadata.Stub(a => a.FieldExists("TitleCode")).Return(true);
            container.RegisterInstance<ILookupDefinitionCache>(lookupDefinitionCache);
            CodeRow[] RequiredValues = new CodeRow[]
            {
                new CodeRow()
                {
                    Code = "1",
                    Description = "Mr",
                    LanguageId = 1
                }
            };
            int p = -1;
            lookupDefinition.Stub(a => a.RetrieveValues(null, 1, 0, -1, LookupOptions.None, out p)).IgnoreArguments().Return(RequiredValues);
            container.RegisterInstance<ILookupDefinition>(lookupDefinition);
            ObjectFactory.Instance = new ObjectFactory(container);
        }

        [TestMethod]
        public void PersonDetailVersionPlugin_PreValidationDefaulting_ListNameDefaulting_WithSurnameAndForename()
        {
            this.Process();
            Assert.AreEqual("f s", this.component.ListName);
        }

        [TestMethod]
        public void PersonDetailVersionPlugin_PreValidationDefaulting_ListNameDefaulting_WithTitleCode()
        {
            this.component.TitleCode = "1";
            this.Process();
            Assert.AreEqual("Mr f s", this.component.ListName);
        }

        [TestMethod]
        public void PersonDetailVersionPlugin_PreValidationDefaulting_ListNameDefaulting_WithoutSurnameAndForename()
        {
            this.component.Surname = null;
            this.component.Forename = null;
            this.Process();
            Assert.AreEqual(string.Empty, this.component.ListName);
        }

        private void Process()
        {
            this.component.Name = new Name(NameType.Person);
            this.component.Name.Context = new IDTransactionContext(Guid.NewGuid().ToString(), string.Empty, "CreateName");
            this.target.ProcessComponent(this.component, ProcessInvocationPoint.PreValidationDefaulting, 1);
        }
    }
}
