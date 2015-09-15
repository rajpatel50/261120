using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Caching;
using Xiap.Framework.Common.DecisionTable;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class PolicyAttachmentAndVehicleTypeValidationPluginTest : StubbedTest
    {
        private ClaimHeader claimHeader;
        private IComponentMetadata componentMetadata;
        private ProcessResultsCollection processResultsCollection;
        private PolicyAttachmentAndVehicleTypeValidationPlugin target;

        [TestInitialize]
        public void TestInitialize()
        {
            this.container = new UnityContainer();
            this.container.RegisterType<IMessageService, MessageService>();
            this.componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            this.container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            this.container.RegisterInstance<IComponentMetadata>(this.componentMetadata);
            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            List<ValueSetCacheName> codes = new List<ValueSetCacheName>();
            codes.Add(new ValueSetCacheName() { Code = "DummyCode", LanguageID = 1, LongDescription = "Description", ShortDescription = "Description" });
            IQueryable<ValueSetCacheName> codeNames = codes.AsQueryable<ValueSetCacheName>();
            metadataMock.Stub(md => md.QueryValueSetNames(Arg<SystemValueSetCodeEnum>.Is.Anything, Arg<int>.Is.Equal(1))).Return(codeNames);
            BusinessComponentKey key = new BusinessComponentKey("TEST");
            key.Add(new BusinessComponentKeyMember("TEST", "1"));
            this.componentMetadata.Stub(a => a.FieldExists("ClaimHeaderAnalysisCode03")).Return(true);
            this.componentMetadata.Stub(a => a.GetField("ClaimHeaderAnalysisCode03")).Return(new Field() { Title = "ClaimHeaderAnalysisCode02", LookupDefinitionKey = key, LookupParameters = new LookupParameters { Code = "TEST" } });

            this.componentMetadata.Stub(a => a.FieldExists("ClaimHeaderAnalysisCode09")).Return(true);
            this.componentMetadata.Stub(a => a.GetField("ClaimHeaderAnalysisCode09")).Return(new Field() { Title = "ClaimHeaderAnalysisCode02", LookupDefinitionKey = key, LookupParameters = new LookupParameters { Code = "TEST" } });
            this.claimHeader = new ClaimHeader();

            ILookupDefinitionCache lookupDefinitionCache = MockRepository.GenerateStub<ILookupDefinitionCache>();
            ILookupDefinition lookupDefinition = MockRepository.GenerateStub<ILookupDefinition>();
            lookupDefinitionCache.Stub(a => a.GetLookupDefinition(string.Empty, 0)).IgnoreArguments().Return(lookupDefinition);
            this.container.RegisterInstance<ILookupDefinitionCache>(lookupDefinitionCache);
            lookupDefinition.Stub(a => a.RetrieveValues(Arg<LookupParameters>.Is.Anything,
                                  Arg<short>.Is.Equal((short)1),
                                  Arg<int>.Is.Anything,
                                  Arg<int?>.Is.Anything,
                                  Arg<LookupOptions>.Is.Equal(LookupOptions.None),
                                  out Arg<int>.Out(new int()).Dummy)).Return((new CodeRow[] { new CodeRow() { Code = "ClaimHeaderAnalysisCode02", Description = "ClaimHeaderAnalysisCode02", LanguageId = 1 } }));
            this.container.RegisterInstance<ILookupDefinition>(lookupDefinition);

            ObjectFactory.Instance = new ObjectFactory(this.container);
            this.processResultsCollection = new ProcessResultsCollection();
        }

        [TestMethod]
        public void AreVehicleTypeAndGeniusVehicleCategory_Mismatch_True()
        {
            string[] list = new string[5] { "T1", StaticValues.PolicyLinkLevel.Coverage.ToString(), "DummyCode", "InvalidCode", "ExternalCovRef" };
            this.claimHeader.ClaimHeaderAnalysisCode09 = "ValidCode";
            this.target = new PolicyAttachmentAndVehicleTypeValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = list });
            Assert.AreEqual(this.processResultsCollection.Count, 1);
            AssertEx.ContainsMessage(this.processResultsCollection, ClaimConstants.VEHICLE_TYPE_AND_GENIUS_VEHICLE_TYPE_MISMATCH);
        }

        [TestMethod]
        public void AreVehicleTypeAndGeniusVehicleCategory_Mismatch_False()
        {
            string[] list = new string[5] { "T1", StaticValues.PolicyLinkLevel.Coverage.ToString(), "DummyCode", "ValidCode", "ExternalCovRef" };
            this.claimHeader.ClaimHeaderAnalysisCode09 = "ValidCode";
            this.target = new PolicyAttachmentAndVehicleTypeValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = list });
            Assert.AreEqual(this.processResultsCollection.Count, 0);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
