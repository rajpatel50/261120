using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Configuration;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.Framework.Common.Product;
using Xiap.Framework.Logging;
using Xiap.Testing.Utils.Mocks;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class GeniusPolicyFieldAttachmentPluginTest : ComponentPluginBaseTest<GeniusPolicyFieldAttachmentPlugin>
    {
        private ClaimHeader component;

        private void TestInitializeVariables(ClaimHeader component, ProductNameInvolvement prodName, bool setPolicyHeaderID)
        {
            target = new GeniusPolicyFieldAttachmentPlugin();

            ChangedProperty dt = new ChangedProperty(null, "XUK0020850MO11A");

            if (setPolicyHeaderID)
            {
                component.PropertiesChanged.Add("PolicyHeaderID", dt);
            }

            UWHeaderData uwHeaderData = new UWHeaderData() {HeaderReference = "XUK000512612MO", HeaderID = 2};
            UWSectionData sectionData = new UWSectionData();
            UWSectionDetailData sectionDetailData = new UWSectionDetailData() { CustomCode03 = "O" };
            UWCoverageData coverageData = new UWCoverageData() { CoverageTitle = "ABC"};
            NameInvolvementData nameInvData = new NameInvolvementData() { NameID = 36 };
            NameInvolvementData nameInvLBData = new NameInvolvementData() { NameID = 338 };
            UWTermsData termsData = new UWTermsData() { VersionNumber = 1, TermsStartDate = DateTime.MinValue, TermsEndDate = DateTime.MaxValue };

            Guid guid = Guid.NewGuid();
            component.Context = new TransactionContext(guid.ToString(), String.Empty, String.Empty);
            component.UWHeader = uwHeaderData;
            component.UWHeader.CustomReference09 = "GBP";
            component.UWHeader.CustomReference10 = "GBP";
            
            uwHeaderData.Sections = new List<UWSectionData>();
            uwHeaderData.Sections.Add(sectionData);

            sectionData.SectionDetails = new List<UWSectionDetailData>();
            sectionData.SectionDetails.Add(sectionDetailData);

            sectionDetailData.Coverages = new List<UWCoverageData>();
            sectionDetailData.Coverages.Add(coverageData);

            uwHeaderData.Nameinvolvements = new List<NameInvolvementData>();
            uwHeaderData.Nameinvolvements.Add(nameInvData);
            uwHeaderData.Nameinvolvements.Add(nameInvLBData);

            uwHeaderData.Terms = new List<UWTermsData>();
            uwHeaderData.Terms.Add(termsData);

            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();

            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IComponentMetadata>(metadata);


            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            var list = new List<short?>() { 1 };
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            metadataMock.Stub(c => c.GetProductNameInvolvement(3)).Return(prodName);

            IUnderwritingService underwritingService = MockRepository.GenerateStub<IUnderwritingService>();
            container.RegisterInstance<IUnderwritingService>("XIAP", underwritingService);
            underwritingService.Stub(uService => uService.UpdateNameInvolvements(uwHeaderData.HeaderReference, this.component.UWHeader)).Return(null);
            underwritingService.Stub(uService => uService.GetPolicy(uwHeaderData.HeaderReference, null)).Return(uwHeaderData);

            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(configurationManager);

            if (prodName != null)
            {
                var productNameInvolvementDefinition = new Xiap.Metadata.Data.XML.ProductVersion.ProductNameInvolvement()
                {
                    ProductNameInvolvementID = prodName.ProductNameInvolvementID,
                    NameInvolvementType = prodName.NameInvolvementType
                };

                var productNameInvolvementQuery = MockRepository.GenerateStub<IProductNameInvolvementQuery>();
                productNameInvolvementQuery.Stub(i => i.GetProductNameInvolvement(3)).Return(productNameInvolvementDefinition);
                productNameInvolvementQuery.Stub(c => c.GetProductNameInvolvementByNameInvolvementType(3, 18)).Return(productNameInvolvementDefinition);
                productNameInvolvementQuery.Stub(c => c.GetProductNameInvolvementByNameInvolvementType(3, 21)).Return(productNameInvolvementDefinition);
                productNameInvolvementQuery.Stub(c => c.GetProductNameInvolvementByNameInvolvementType(3, 23)).Return(productNameInvolvementDefinition);
                container.RegisterInstance<IProductNameInvolvementQuery>(productNameInvolvementQuery);
            }

            container.RegisterInstance<ILoggingWrapper>(new MockLoggingService());

            ObjectFactory.Instance = new ObjectFactory(container);
        }

        [TestMethod]
        public void AssignGeniusPolicyFieldsWithLossBrokerTest()
        {
            ProductNameInvolvement productNameLB = new ProductNameInvolvement();
            productNameLB.NameInvolvementType = (short)StaticValues.NameInvolvementType.LossBroker;
            productNameLB.ProductNameInvolvementID = 3;
            this.component = new ClaimHeader();

            this.TestInitializeVariables(this.component, productNameLB, true);
            this.component.AddNewClaimInvolvement(StaticValues.LinkableComponentType.NameInvolvement).AddClaimNameInvolvement(productNameLB.ProductNameInvolvementID);
            this.component.ClaimInvolvements.FirstOrDefault().ClaimNameInvolvements.FirstOrDefault().NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest;
            ProcessInvocationPoint point = ProcessInvocationPoint.ComponentChange;
            ProcessResultsCollection result = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsTrue(this.component.ClaimInvolvements.Any(x => x.ClaimNameInvolvements.Where(y => y.NameInvolvementType == (short)StaticValues.NameInvolvementType.LossBroker).Count() > 0));
        }

        [TestMethod]
        public void AssignGeniusPolicyFieldsWithMajorInsuredTest()
        {
            ProductNameInvolvement prodNameCLMT = new ProductNameInvolvement();
            prodNameCLMT.NameInvolvementType = (short)StaticValues.NameInvolvementType.MajorInsured;
            prodNameCLMT.ProductNameInvolvementID = 3;
            this.component = new ClaimHeader();

            this.TestInitializeVariables(this.component, prodNameCLMT, true);
            this.component.AddNewClaimInvolvement(StaticValues.LinkableComponentType.NameInvolvement).AddClaimNameInvolvement(prodNameCLMT.ProductNameInvolvementID);
            this.component.ClaimInvolvements.FirstOrDefault().ClaimNameInvolvements.FirstOrDefault().NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest;
            ProcessInvocationPoint point = ProcessInvocationPoint.ComponentChange;
            ProcessResultsCollection result = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsTrue(this.component.ClaimInvolvements.Any(x => x.ClaimNameInvolvements.Where(y => y.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured).Count() > 0));
        }

        [TestMethod]
        public void AssignGeniusPolicyFieldsWhenHdrHasMajorClaimantTest()
        {
            ProductNameInvolvement prodNameCLMT = new ProductNameInvolvement();
            prodNameCLMT.NameInvolvementType = (short)StaticValues.NameInvolvementType.MajorClaimant;
            prodNameCLMT.ProductNameInvolvementID = 3;
            this.component = new ClaimHeader();

            this.TestInitializeVariables(this.component, prodNameCLMT, true);
            this.component.AddNewClaimInvolvement(StaticValues.LinkableComponentType.NameInvolvement).AddClaimNameInvolvement(prodNameCLMT.ProductNameInvolvementID);
            this.component.ClaimInvolvements.FirstOrDefault().ClaimNameInvolvements.FirstOrDefault().NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest;

            ProcessInvocationPoint point = ProcessInvocationPoint.ComponentChange;
            ProcessResultsCollection result = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsTrue(this.component.ClaimInvolvements.Any(x => x.ClaimNameInvolvements.Where(y => y.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorClaimant).Count() > 0));
        }

        [TestMethod]
        public void SetDateOfEventFromDateOfLossTest()
        {
            this.component = new ClaimHeader();
            this.component.DateOfLossFrom = DateTime.Today.Date;
            this.component.DateOfEventFrom = null;
            this.TestInitializeVariables(this.component, null, true);
            ProcessInvocationPoint point = ProcessInvocationPoint.Created;
            ProcessResultsCollection result = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(this.component.DateOfEventFrom, this.component.DateOfLossFrom);
        }

        [TestMethod]
        public void ComponentChangeDefaultingTest()
        {
            this.component = new ClaimHeader() { DateOfLossFrom = DateTime.Today.Date, DateOfEventFrom = DateTime.Today.Date.AddDays(2), DateOfLossTypeCode = "O" };
            this.component.PropertiesChanged = new Dictionary<string, ChangedProperty>();
            ChangedProperty cp = new ChangedProperty(DateTime.Now, DateTime.Now.AddDays(1));
            this.component.PropertiesChanged.Add("DateOfEventFrom", cp);
            this.TestInitializeVariables(this.component, null, true);
            this.component.PolicyHeaderID = null;
            ProcessInvocationPoint point = ProcessInvocationPoint.ComponentChange;
            ProcessResultsCollection result = target.ProcessComponent(this.component, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(this.component.DateOfLossFrom, this.component.DateOfEventFrom);
        }

        [TestMethod]
        public void DateOfLossFromFieldRetrieval_SetReadOnlyTrueTest()
        {
            this.component = new ClaimHeader() { PolicyHeaderID = 120};
            this.TestInitializeVariables(this.component, null, false);
            this.component.Context = new TransactionContext(Guid.NewGuid().ToString(), null, "AmendClaim");
            Field field = new Field();
            field.PropertyName = "DateOfLossFrom";
            field.ConfigurableFieldID = 1;
            Assert.IsFalse(field.Readonly);
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Readonly);
        }

        [TestMethod]
        public void DateOfLossTypeCodeFieldRetrieval_SetReadOnlyTrueTest()
        {
            this.component = new ClaimHeader() { PolicyHeaderID = 120, DateOfLossTypeCode = "O" };
            this.TestInitializeVariables(this.component, null, false);
            this.component.Context = new TransactionContext(Guid.NewGuid().ToString(), null, "AmendClaim");
            Field field = new Field();
            field.PropertyName = "DateOfLossTypeCode";
            field.ConfigurableFieldID = 1;
            Assert.IsFalse(field.Readonly);
            target.FieldRetrieval(this.component, ProcessInvocationPoint.FieldRetrieval, ref field, 0);
            Assert.IsTrue(field.Readonly);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
