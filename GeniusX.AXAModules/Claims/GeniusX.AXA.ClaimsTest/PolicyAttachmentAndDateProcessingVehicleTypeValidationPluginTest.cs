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
using Xiap.Testing.Utils;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class PolicyAttachmentAndDateProcessingVehicleTypeValidationPluginTest : ComponentPluginBaseTest<PolicyAttachmentAndDateProcessingVehicleTypeValidationPlugin>
    {
        private ClaimHeader claimHeader;
        private ProcessResultsCollection processResultsCollection;
        private IComponentMetadata componentMetadata;


        [TestInitialize]
        public void TestInitialize()
        {
            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            this.componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            container.RegisterInstance<IComponentMetadata>(this.componentMetadata);
            this.claimHeader = new ClaimHeader();
            this.componentMetadata.Stub(a => a.FieldExists("ClaimHeaderAnalysisCode09")).Return(true);
            this.componentMetadata.Stub(a => a.GetField("ClaimHeaderAnalysisCode09")).Return(new Field() { Title = "Vehicle type", ShortTitle = "Vehicle type" });
            ObjectFactory.Instance = new ObjectFactory(container);
            this.processResultsCollection = new ProcessResultsCollection();
        }

        [TestMethod]
        public void   VehicleType_Mandatory_True()
        {
            this.target = new PolicyAttachmentAndDateProcessingVehicleTypeValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 1);
        }

        
        [TestMethod]
        public void   VehicleType_Mandatory_False()
        {
            this.claimHeader.ClaimHeaderAnalysisCode09 = "TEST";
            this.target = new PolicyAttachmentAndDateProcessingVehicleTypeValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 0);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
