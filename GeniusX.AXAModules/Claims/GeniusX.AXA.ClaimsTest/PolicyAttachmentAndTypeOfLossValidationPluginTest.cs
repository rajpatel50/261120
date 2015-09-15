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
using Xiap.Framework.Data.Underwriting;
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
    public class PolicyAttachmentAndTypeOfLossValidationPluginTest : StubbedTest
    {
        private ClaimHeader claimHeader;
        private IDecisionTableHelper decisionTableHelper;
        private IComponentMetadata componentMetadata;
        private ProcessResultsCollection processResultsCollection;
        private IDecisionTableComponent decisionTableComponent;
        private PolicyAttachmentAndTypeOfLossValidationPlugin target;
        private IUnderwritingService mockIUnderwritingService;
        private string[] list = new string[5] { "T1", StaticValues.PolicyLinkLevel.Coverage.ToString(), "DummyCode", "TEST", "ExternalCovRef" };


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
            this.componentMetadata.Stub(a => a.FieldExists("ClaimHeaderAnalysisCode02")).Return(true);
            this.componentMetadata.Stub(a => a.GetField("ClaimHeaderAnalysisCode02")).Return(new Field() { Title = "ClaimHeaderAnalysisCode02", LookupDefinitionKey = key, LookupParameters = new LookupParameters { Code = "TEST" } });

            this.decisionTableHelper = MockRepository.GenerateStub<IDecisionTableHelper>();
            this.container.RegisterInstance<IDecisionTableHelper>(this.decisionTableHelper);
            this.decisionTableComponent = new DecisionTableComponent { Context = new TransactionContext(string.Empty, string.Empty, string.Empty) };

            this.decisionTableHelper.Stub(s => s.Call(Arg<string>.Is.Anything, Arg<DateTime>.Is.Anything, Arg<object[]>.Is.Anything)).Return(this.decisionTableComponent);
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

            this.mockIUnderwritingService = MockRepository.GenerateStub<IUnderwritingService>();
            container.RegisterInstance<IUnderwritingService>(this.mockIUnderwritingService);
            ObjectFactory.Instance = new ObjectFactory(this.container);
            this.processResultsCollection = new ProcessResultsCollection();
        }



        [TestMethod]
        public void ValidateTypeOfLossAndCoverageTypeCombination_Mismatch_True()
        {
            this.mockIUnderwritingService.Stub(a => a.GetCoverageIDByExternalReference(Arg<string>.Is.Anything)).Return(1);
            this.claimHeader.ClaimHeaderAnalysisCode02 = "ClaimHeaderAnalysisCode02";
            this.decisionTableComponent.IsValid = false;
            this.target = new PolicyAttachmentAndTypeOfLossValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = this.list });
            Assert.AreEqual(this.processResultsCollection.Count, 1);
            AssertEx.ContainsMessage(this.processResultsCollection, ClaimConstants.TYPE_OF_LOSS_AND_COVERAGE_TYPE_MISMATCH);
        }

        [TestMethod]
        public void ValidateTypeOfLossAndCoverageTypeCombination_Mismatch_False()
        {
            this.mockIUnderwritingService.Stub(a => a.GetCoverageIDByExternalReference(Arg<string>.Is.Anything)).Return(1);
            this.decisionTableComponent.IsValid = true;
            this.target = new PolicyAttachmentAndTypeOfLossValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = this.list });
            Assert.AreEqual(this.processResultsCollection.Count, 0);
        }

        [TestMethod]
        public void IsExternalPolicyChild_ComponentExists_True()
        {
            this.mockIUnderwritingService.Stub(a => a.GetCoverageIDByExternalReference(Arg<string>.Is.Anything)).Return(1);
            this.decisionTableComponent.IsValid = true;
            this.target = new PolicyAttachmentAndTypeOfLossValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = this.list });
            Assert.AreEqual(this.processResultsCollection.Count, 0);
        }

        [TestMethod]
        public void IsExternalPolicyChild_ComponentExists_False()
        {
            this.mockIUnderwritingService.Stub(a => a.GetCoverageIDByExternalReference(Arg<string>.Is.Anything)).Return(0);
            this.decisionTableComponent.IsValid = true;
            this.target = new PolicyAttachmentAndTypeOfLossValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = this.list });
            Assert.AreEqual(this.processResultsCollection.Count, 1);
            AssertEx.ContainsMessage(this.processResultsCollection, ClaimConstants.POLICY_ATTACHMENT_NOT_ALLOWED);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "External Reference should not be null or empty.")]
        public void External_Reference_Null()
        {
            string[] list1 = new string[5] { string.Empty, StaticValues.PolicyLinkLevel.Coverage.ToString(), "DummyCode", "TEST", "ExternalCovRef" };
            this.mockIUnderwritingService.Stub(a => a.GetCoverageIDByExternalReference(Arg<string>.Is.Anything)).Return(0);
            this.decisionTableComponent.IsValid = true;
            this.target = new PolicyAttachmentAndTypeOfLossValidationPlugin();
            this.target.ProcessComponent(this.claimHeader, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = list1 });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "PolicyLinkLevel has an invalid value.")]
        public void PolicyLinkLevel_InvalidValue_True()
        {
            string[] list1 = new string[5] { string.Empty, "PolicyLinkLevel_InvalidValue", "DummyCode", "TEST", "ExternalCovRef" };
            this.target = new PolicyAttachmentAndTypeOfLossValidationPlugin();
            this.target.ProcessComponent(this.claimHeader, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = list1 });
        }

        [TestCleanup]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
