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
    public class PolicyAttachmentAndDateProcessingValidationPluginTest : ComponentPluginBaseTest<PolicyAttachmentAndDateProcessingValidationPlugin>
    {
        private ClaimHeader claimHeader;
        private ProcessResultsCollection processResultsCollection;
        private IComponentMetadata componentMetadata;
        public const string EMPTY_GROUP_CODE = "XEMPTY";
        public const string MOTOR_PRODUCT_CODE = "CGBIMO";

        [TestInitialize]
        public void TestInitialize()
        {
            this.claimHeader = new ClaimHeader();
            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            this.componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            container.RegisterInstance<IComponentMetadata>(this.componentMetadata);
            ObjectFactory.Instance = new ObjectFactory(container);
            this.processResultsCollection = new ProcessResultsCollection();
        }

        private void AddClaimDetails()
        {
            ClaimDetail claimDetail = new ClaimDetail();
            this.claimHeader.InternalClaimDetails.Add(claimDetail);
        }

        private void SetStubedField(string divisionGroupCode)
        {
            this.componentMetadata.Stub(a => a.FieldExists("ClaimHeaderAnalysisCode04")).Return(true);
            this.componentMetadata.Stub(a => a.GetField("ClaimHeaderAnalysisCode04")).Return(new Field() { Title = "Division", ShortTitle = "Division", LookupParameters = new LookupParameters { GroupCode = divisionGroupCode } });
        }

        private void AddInsuredNameInvolvement()
        {
            ClaimNameInvolvement nameInvolvement = new ClaimNameInvolvement();
            nameInvolvement.NameID = 1;
            nameInvolvement.NameInvolvementType = (short)StaticValues.NameInvolvementType.MajorInsured;
            nameInvolvement.NameUsageTypeCode = "INS";
            nameInvolvement.NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest;
            ClaimInvolvement invo = new ClaimInvolvement();
            invo.ClaimInvolvementType = (short?)StaticValues.LinkableComponentType.NameInvolvement;
            invo.InternalClaimNameInvolvements.Add(nameInvolvement);
            this.claimHeader.InternalClaimInvolvements.Add(invo);
        }

        [TestMethod]
        public void PerformPreCoverageVerificationCustomValidations_MajorInsuredMandatory_True()
        {
            this.AddClaimDetails();
            this.SetStubedField(EMPTY_GROUP_CODE);
            this.claimHeader.ClaimHeaderAnalysisCode04 = "TEST";
            this.target = new PolicyAttachmentAndDateProcessingValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 1);
            AssertEx.ContainsMessage(this.processResultsCollection, MessageConstants.MANDATORY_FIELD_NOT_SPECIFIED);
        }

        [TestMethod]
        public void PerformPreCoverageVerificationCustomValidations_MajorInsuredMandatory_False()
        {
            this.AddClaimDetails();
            this.AddInsuredNameInvolvement();
            this.SetStubedField(EMPTY_GROUP_CODE);
            this.claimHeader.ClaimHeaderAnalysisCode04 = "TEST";
            this.target = new PolicyAttachmentAndDateProcessingValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 0);
        }


        [TestMethod]
        public void PerformPreCoverageVerificationCustomValidations_DivisionMandatory_True()
        {
            this.AddClaimDetails();
            this.AddInsuredNameInvolvement();
            this.SetStubedField("TEST");
            this.target = new PolicyAttachmentAndDateProcessingValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 1);
            AssertEx.ContainsMessage(this.processResultsCollection, MessageConstants.MANDATORY_FIELD_NOT_SPECIFIED);
        }

        [TestMethod]
        public void PerformPreCoverageVerificationCustomValidations_DivisionMandatory_False()
        {
            this.AddClaimDetails();
            this.AddInsuredNameInvolvement();
            this.SetStubedField(EMPTY_GROUP_CODE);
            this.claimHeader.ClaimHeaderAnalysisCode04 = "TEST";
            this.claimHeader.ClaimHeaderAnalysisCode09 = "TEST";
            this.target = new PolicyAttachmentAndDateProcessingValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 0);
        }

        [TestMethod]
        public void PerformPreCoverageVerificationCustomValidations_ClaimDetailMandatory_True()
        {
            this.AddInsuredNameInvolvement();
            this.SetStubedField(EMPTY_GROUP_CODE);
            this.claimHeader.ClaimHeaderAnalysisCode04 = "TEST";
            this.claimHeader.ClaimHeaderAnalysisCode09 = "TEST";
            this.target = new PolicyAttachmentAndDateProcessingValidationPlugin();
            this.processResultsCollection = this.target.ProcessComponent(this.claimHeader, ProcessInvocationPoint.Virtual, 0);
            Assert.IsTrue(this.processResultsCollection.Count == 1);
            AssertEx.ContainsMessage(this.processResultsCollection, ClaimConstants.AT_LEAST_ONE_UNATTACHED_CLAIM_DETAIL_MUST_EXISTS);
        }

        [TestMethod]
        public void PerformPreCoverageVerificationCustomValidations_ClaimDetailMandatory_False()
        {
            this.AddInsuredNameInvolvement();
            this.AddClaimDetails();
            this.SetStubedField(EMPTY_GROUP_CODE);
            this.claimHeader.ClaimHeaderAnalysisCode04 = "TEST";
            this.claimHeader.ClaimHeaderAnalysisCode09 = "TEST";
            this.target = new PolicyAttachmentAndDateProcessingValidationPlugin();
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