using System;
using GeniusX.AXA.Underwriting.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Testing.Utils;
using Xiap.Testing.Utils.Mocks;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Underwriting.Test
{
    //// <summary>
    //// Summary description for UnitTest1
    //// </summary>
    [TestClass]
    public class ExcessAndDeductibleDataSetValidationPluginTest
    {
       private ITransactionPlugin plugin;
       private  IBusinessTransaction businessTransaction;
       private Header header;
       private  HeaderVersion headerVersion;
       private  SectionDetailVersion sectionDetailVersion;
       private CoverageVersion coverageVersion;
       private IComponentMetadata componentMetadata;
        
        [TestInitialize]
        public void Initialise()
        {
           this.plugin = new ExcessAndDeductibleDataSetValidationPlugin();
           this.componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
          // var headerVersionBuilder = new BusinessComponentBuilder<HeaderVersion>().Build();
          // var sdvBuilder = new BusinessComponentBuilder<SectionDetailVersion>().Build();
          // var coverageBuilder = new BusinessComponentBuilder<CoverageVersion>().Build();
            var hdr = new BusinessComponentBuilder<Header>()
                .Add(new BusinessComponentBuilder<HeaderVersion>()
                    .SetProperty(a => a.IsLatestVersion = true))
                .Add(new BusinessComponentBuilder<Section>()
                    .Add(new BusinessComponentBuilder<SectionVersion>()
                        .SetProperty(a => a.IsLatestVersion = true))
                    .Add(new BusinessComponentBuilder<SectionDetail>()
                        .Add(new BusinessComponentBuilder<SectionDetailVersion>()
                            .SetProperty(a => a.IsLatestVersion = true))
                        .Add(new BusinessComponentBuilder<Coverage>()
                            .Add(new BusinessComponentBuilder<CoverageVersion>()
                                .SetProperty(a => a.IsLatestVersion = true))))).Build();

            this.componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            var genericDataTypeVersion = new ProductBuilder<GenericDataTypeVersion>(this.componentMetadata).Build();
            genericDataTypeVersion.GenericDataTypeComponent = new GenericDataType { Code = "AND2" };

            this.header = hdr;
            this.headerVersion = this.header.HeaderVersions[0];
            this.headerVersion.CreateGenericDataSet();
            this.sectionDetailVersion = this.header.Sections[0].SectionDetails[0].SectionDetailVersions[0];
            this.sectionDetailVersion.CreateGenericDataSet();
            this.coverageVersion = this.header.Sections[0].SectionDetails[0].Coverages[0].CoverageVersions[0];
            this.coverageVersion.CreateGenericDataSet();

            this.businessTransaction = MockRepository.GenerateStub<IBusinessTransaction>();
            this.businessTransaction.Component = hdr;

            var metadata = MockRepository.GenerateStub<IMetadataQuery>();
            metadata.Stub(a => a.GetGenericDataTypeVersion(0, DateTime.Now)).IgnoreArguments().Return(new GenericDataTypeVersion { GenericDataTypeVersionID = 0 });
            var container = new UnityContainer();
            container.RegisterInstance<IMetadataQuery>(metadata);
            container.RegisterInstance<IComponentMetadata>(this.componentMetadata);
            container.RegisterInstance<IMessageService>(new MockMessagingService());
            ObjectFactory.Instance = new ObjectFactory(container);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_NoDeductibles_NoProcessResults()
        {
            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_SingleDeductibleAllSubCodesBlank_NoProcessResults()
        {
            this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_SingleDeductibleSubTypeVehicle_NoProcessResult()
        {
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode02 = "bob";

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_SingleDeductibleSubTypeDivision_NoProcessResult()
        {
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode01 = "bob";

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_SingleDeductibleSubTypeDeductible_ProcessResult()
        {
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode03 = "bob";

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_MultipleDeductiblesSubTypeVehicle_NoProcessResults()
        {
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode02 = "bobA";
            dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode02 = "bobB";

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_MultipleDeductiblesSubTypeDivision_NoProcessResults()
        {
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode01 = "bobA";
            dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode01 = "bobB";

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_MultipleDeductiblesSubTypeDeductible_NoProcessResults()
        {
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode03 = "bobA";
            dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode03 = "bobB";

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_MultipleDeductiblesMultipeSubTypes_ProcessResult()
        {
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode02 = "bobA";
            dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomCode03 = "bobB";

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(1, processResults.Count);
            Assert.AreEqual("DataItemsRepresentMultipleSubSections", processResults.Results[0].Message);
        }

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_MultipleDeductiblesNoSubTypes_NoProcessResult()
        {
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomNumeric01 = 1;
            dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomNumeric01 = 2;

            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(0, processResults.Count);
        }

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductiblesOneSubTypeOneNot_ProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobA";
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(1, processResults.Count);
			Assert.AreEqual("DataItemsRepresentMultipleSubSections", processResults.Results[0].Message);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleNoSubTypeDiffSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomNumeric01 = 2;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleSameSubTypeDivDiffSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode01 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode01 = "bobA";
			dataItem.CustomNumeric01 = 2;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleSameSubTypeVehDiffSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobA";
			dataItem.CustomNumeric01 = 2;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleSameSubTypeDedDiffSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode03 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode03 = "bobA";
			dataItem.CustomNumeric01 = 2;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleDiffSubTypeDivDiffSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode01 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode01 = "bobB";
			dataItem.CustomNumeric01 = 2;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleDiffSubTypeVehDiffSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobB";
			dataItem.CustomNumeric01 = 2;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleDiffSubTypeDedDiffSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode03 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode03 = "bobB";
			dataItem.CustomNumeric01 = 2;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleDiffSubTypeDivSameSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode01 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode01 = "bobB";
			dataItem.CustomNumeric01 = 1;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleDiffSubTypeVehSameSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobB";
			dataItem.CustomNumeric01 = 1;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleDiffSubTypeDedSameSeq_NoProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode03 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode03 = "bobB";
			dataItem.CustomNumeric01 = 1;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(0, processResults.Count);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleNoSubTypeSameSeq_ProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomNumeric01 = 1;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(1, processResults.Count);
			Assert.AreEqual("DeductibleSequenceMustBeDistinct", processResults.Results[0].Message);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleSameSubTypeDivSameSeq_ProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode01 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode01 = "bobA";
			dataItem.CustomNumeric01 = 1;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(1, processResults.Count);
			Assert.AreEqual("DeductibleSequenceMustBeDistinct", processResults.Results[0].Message);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleSameSubTypeVehSameSeq_ProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode02 = "bobA";
			dataItem.CustomNumeric01 = 1;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(1, processResults.Count);
			Assert.AreEqual("DeductibleSequenceMustBeDistinct", processResults.Results[0].Message);
		}

		[TestMethod]
		public void ProcessTransaction_PolicyLevel_MultipleDeductibleSameSubTypeDedSameSeq_ProcessResult()
		{
			var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode03 = "bobA";
			dataItem.CustomNumeric01 = 1;
			dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
			dataItem.CustomCode03 = "bobA";
			dataItem.CustomNumeric01 = 1;

			var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
			Assert.AreEqual(1, processResults.Count);
			Assert.AreEqual("DeductibleSequenceMustBeDistinct", processResults.Results[0].Message);
		}

        [TestMethod]
        public void ProcessTransaction_PolicyLevel_Deductible_Policy_Reference_Should_Not_Be_Null_ProcessResult()
        {
            this.componentMetadata.Stub(m => m.FieldExists("CustomReference01")).IgnoreArguments().Return(true);
            Field field = new Field() { PropertyName = "CustomReference01", TypeInfo = new DataTypeInfo(DataTypeEnum.String), Title = "Deductible Policy Reference 1" };
            this.componentMetadata.Stub(m => m.GetField("CustomReference01")).Return(field);
            var dataItem = this.headerVersion.GenericDataSet.AddGenericDataItem(0, DateTime.Now);
            dataItem.CustomNumeric03 = 1;
            this.headerVersion.CustomReference01 = null;
            var processResults = this.plugin.ProcessTransaction(this.businessTransaction, TransactionInvocationPoint.PostValidate, 1);
            Assert.AreEqual(1, processResults.Count);
            Assert.AreEqual(UwMessageConstants.DEDUCTIBLE_POLICY_REFERENCE_SHOULD_NOT_BE_NULL, processResults.Results[0].Message);
        }
	}
}
