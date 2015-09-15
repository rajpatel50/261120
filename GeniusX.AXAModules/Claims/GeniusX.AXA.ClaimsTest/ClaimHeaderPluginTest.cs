using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Objects;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common.Product;
using Xiap.Framework.Configuration;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ClaimHeaderPluginTest : ComponentPluginBaseTest<ClaimHeaderPlugin>
    {
        private ClaimHeader component;

		[TestInitialize]
		public void TestInitialize()
		{
            this.component = new ClaimHeader();

            this.component.CustomBoolean15 = false;
            this.component.CustomNumeric10 = 0;

			ClaimDetail cd1 = new ClaimDetail();
			cd1.ClaimDetailID = 1;
			cd1.IsDeductible01PaidByInsurer = false;
			cd1.PolicyDeductible01 = 0;
			cd1.ProductClaimDetailID = 1;
            this.component.InternalClaimDetails.Add(cd1);

			ClaimDetail cd2 = new ClaimDetail();
			cd2.ClaimDetailID = 2;
			cd1.IsDeductible01PaidByInsurer = false;
			cd1.PolicyDeductible01 = 0;
			cd2.ProductClaimDetailID = 1;
            this.component.InternalClaimDetails.Add(cd2);

			ProductClaimDetail pcd = new ProductClaimDetail();
			pcd.ProductClaimDetailID = 1;

			IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();
			metadata.Stub(m => m.GetDefinitionComponent<ProductClaimDetail>()).Return(pcd);
			metadata.Stub(m => m.DefinitionComponent).Return(pcd);

			UnityContainer container = new UnityContainer();
			container.RegisterType<IMessageService, MessageService>();
			container.RegisterInstance<IComponentMetadata>(metadata);

			var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();

			container.RegisterInstance<IMetadataQuery>(metadataMock);

			IConfigurationManager icm = MockRepository.GenerateStub<IConfigurationManager>();
			container.RegisterInstance<IConfigurationManager>(icm);
			NameValueCollection appsetting = new NameValueCollection();
			icm.Stub(c => c.AppSettings).Return(appsetting);

			container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
			container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });


            ProductXML.ProductClaimDetail productClaimDetail = new ProductXML.ProductClaimDetail { ProductClaimDetailID = 1 };
            productClaimDetail.ProductClaimDetailToComponentLinks = new ProductXML.ProductClaimDetailToComponentLink[1]
                                                                    {
                                                                        new ProductXML.ProductClaimDetailToComponentLink() {ProductLinkableComponentID =1 }
                                                                    };

            List<ProductXML.ProductClaimDetail> productClaimDetails = new List<ProductXML.ProductClaimDetail> { productClaimDetail };
            IProductClaimDetailQuery claimDetailQuery = MockRepository.GenerateStub<IProductClaimDetailQuery>();
            container.RegisterInstance<IProductClaimDetailQuery>(claimDetailQuery);
            claimDetailQuery.Stub(x => x.GetProductClaimDetails(Arg<long>.Is.Anything)).Return(productClaimDetails);
            claimDetailQuery.Stub(x => x.GetProductClaimDetail(Arg<long>.Is.Anything)).Return(productClaimDetails.First());
            claimDetailQuery.Stub(x => x.GetProductClaimDetail(Arg<long>.Is.Anything, Arg<string>.Is.Anything)).Return(productClaimDetails.First());

			ObjectFactory.Instance = new ObjectFactory(container);

            ClaimsTransactionContext context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);
            this.component.Context = context;
		}

		[TestCleanup()]
		public void TestCleanup()
		{
			ObjectFactory.Instance = null;
		}

		[TestMethod]
		public void DeductiblePropertyChangeClaimHeaderToDetail_NoClaimDetails_NoDataCopied()
		{
			ClaimsTransactionContext context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);
			
			ClaimHeader ch = new ClaimHeader();
			ch.Context = context;
			ch.CustomBoolean15 = false;
			ch.CustomNumeric10 = 0;

			ClaimHeaderPlugin chp = new ClaimHeaderPlugin();
			chp.PropertyChange(ch, ProcessInvocationPoint.PropertyChange, "CustomReference01", false, true, 0);
		}

		[TestMethod]
		public void DeductiblePropertyChangeClaimHeaderToDetail_NoFieldChanged_NoDataCopied()
		{
            this.component.CustomBoolean15 = true;
            this.component.CustomNumeric10 = 50;

            ClaimDetail cd = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 2).Single();
			cd.IsDeductible01PaidByInsurer = false;
			cd.PolicyDeductible01 = 0;
			cd.IsAutomaticDeductibleProcessingApplied = true;

			cd.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod = (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible;

			ClaimHeaderPlugin chp = new ClaimHeaderPlugin();
            chp.PropertyChange(this.component, ProcessInvocationPoint.PropertyChange, "CustomReference01", false, true, 0);

            cd = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 1).Single();
			Assert.AreEqual(false, cd.IsDeductible01PaidByInsurer);
			Assert.AreEqual(0, cd.PolicyDeductible01);

            ClaimDetail cd1 = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 2).Single();
			Assert.AreEqual(false, cd1.IsDeductible01PaidByInsurer);
			Assert.AreEqual(0, cd1.PolicyDeductible01);
		}

		[TestMethod]
		public void DeductiblePropertyChangeClaimHeaderToDetail_NotStandardMethod_NoDataCopied()
		{
            this.component.CustomBoolean15 = true;
            this.component.CustomNumeric10 = 50;

            ClaimDetail cd = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 2).Single();
			cd.IsDeductible01PaidByInsurer = false;
			cd.PolicyDeductible01 = 0;
			cd.IsAutomaticDeductibleProcessingApplied = true;

			cd.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod = (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.FromClaimHeader;

			ClaimHeaderPlugin chp = new ClaimHeaderPlugin();
            chp.PropertyChange(this.component, ProcessInvocationPoint.PropertyChange, "CustomBoolean15", false, true, 0);
            chp.PropertyChange(this.component, ProcessInvocationPoint.PropertyChange, "CustomNumeric10", 0, 50, 0);

            cd = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 1).Single();
			Assert.AreEqual(false, cd.IsDeductible01PaidByInsurer);
			Assert.AreEqual(0, cd.PolicyDeductible01);

            ClaimDetail cd1 = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 2).Single();
			Assert.AreEqual(false, cd1.IsDeductible01PaidByInsurer);
			Assert.AreEqual(0, cd1.PolicyDeductible01);
		}

		[TestMethod]
		public void DeductiblePropertyChangeClaimHeaderToDetail_NotAutomaticDeductible_NoDataCopied()
		{
            this.component.CustomBoolean15 = true;
            this.component.CustomNumeric10 = 50;

            ClaimDetail cd = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 2).Single();
			cd.IsDeductible01PaidByInsurer = false;
			cd.PolicyDeductible01 = 0;
			cd.IsAutomaticDeductibleProcessingApplied = false;

			cd.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod = (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible;

			ClaimHeaderPlugin chp = new ClaimHeaderPlugin();
            chp.PropertyChange(this.component, ProcessInvocationPoint.PropertyChange, "CustomBoolean15", false, true, 0);
            chp.PropertyChange(this.component, ProcessInvocationPoint.PropertyChange, "CustomNumeric10", 0, 50, 0);

            cd = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 1).Single();
			Assert.AreEqual(false, cd.IsDeductible01PaidByInsurer);
			Assert.AreEqual(0, cd.PolicyDeductible01);

            ClaimDetail cd1 = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 2).Single();
			Assert.AreEqual(false, cd1.IsDeductible01PaidByInsurer);
			Assert.AreEqual(0, cd1.PolicyDeductible01);
		}

		[TestMethod]
		public void DeductiblePropertyChangeClaimHeaderToDetail_FieldsChangedAndFlagsCorrect_DataCopied()
		{
            this.component.CustomBoolean15 = true;
            this.component.CustomNumeric10 = 50;

            ClaimDetail cd = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 2).Single();
			cd.IsDeductible01PaidByInsurer = false;
			cd.PolicyDeductible01 = 0;
			cd.IsAutomaticDeductibleProcessingApplied = true;
            cd.ClaimDetailStatusCode = "CDO";
            cd.ClaimDetailTypeCode = "AD";

			cd.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod = (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible;

			ClaimHeaderPlugin chp = new ClaimHeaderPlugin();
            chp.PropertyChange(this.component, ProcessInvocationPoint.PropertyChange, "CustomBoolean15", false, true, 0);
            chp.PropertyChange(this.component, ProcessInvocationPoint.PropertyChange, "CustomNumeric10", 0, 50, 0);

            cd = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 1).Single();
			Assert.AreEqual(false, cd.IsDeductible01PaidByInsurer);
			Assert.AreEqual(0, cd.PolicyDeductible01);

            ClaimDetail cd1 = this.component.ClaimDetails.Where(a => a.ClaimDetailID == 2).Single();
			Assert.AreEqual(true, cd1.IsDeductible01PaidByInsurer);
			Assert.AreEqual(50, cd1.PolicyDeductible01);
		}

        [TestMethod]
        public void SetCreatedByFieldOnClaimHeaderCreated()
        {
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            target = new GeniusX.AXA.Claims.BusinessLogic.ClaimHeaderPlugin();
            ProcessResultsCollection results = target.ProcessComponent(this.component, point, 0);
            Assert.IsNotNull(this.component.CustomReference05);
            Assert.AreEqual(this.component.CustomReference05, (this.component as ClaimHeader).Context.User.UserIdentity);
        }
    }
}
