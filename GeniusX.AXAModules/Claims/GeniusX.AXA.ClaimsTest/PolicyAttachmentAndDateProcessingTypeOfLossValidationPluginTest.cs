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
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class PolicyAttachmentAndDateProcessingTypeOfLossValidationPluginTest : ComponentPluginBaseTest<PolicyAttachmentAndDateProcessingTypeOfLossValidationPlugin>
    {
        private ClaimHeader claimHeader;
        private ProcessResultsCollection processResultsCollection;
        private IComponentMetadata componentMetadata;

        [TestInitialize]
        public void TestInitialize()
        {
            this.claimHeader = new ClaimHeader();
            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            this.componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            container.RegisterInstance<IComponentMetadata>(this.componentMetadata);
            this.componentMetadata.Stub(a => a.FieldExists("ClaimHeaderAnalysisCode02")).Return(true);
            this.componentMetadata.Stub(a => a.GetField("ClaimHeaderAnalysisCode02")).Return(new Field() { Title = "Type of Loss", ShortTitle = "Type of Loss" });
            ObjectFactory.Instance = new ObjectFactory(container);
            this.processResultsCollection = new ProcessResultsCollection();
        }

        [TestMethod]
        public void PerformPreCoverageVerificationCustomValidations_TypeofLossMandatory_True()
        {
            this.target = new PolicyAttachmentAndDateProcessingTypeOfLossValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 1);
            AssertEx.ContainsMessage(this.processResultsCollection, MessageConstants.MANDATORY_FIELD_NOT_SPECIFIED);
        }

        [TestMethod]
        public void PerformPreCoverageVerificationCustomValidations_TypeofLossMandatory_False()
        {
            this.claimHeader.ClaimHeaderAnalysisCode02 = "TEST";
            this.target = new PolicyAttachmentAndDateProcessingTypeOfLossValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 0);
        }
    }
}
