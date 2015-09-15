using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xiap.Testing.Utils;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Xiap.Claims.BusinessComponent;
using Xiap.Metadata.BusinessComponent;
using Rhino.Mocks;
using Xiap.Framework.Messages;
using System.Data.Objects;
using Xiap.Framework;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class AXACalculateDeductiblePluginTest : ComponentPluginBaseTest<AXACalculateDeductiblePlugin>
    {
        private ClaimHeader header;
        private ClaimTransactionHeader ctheader;

        [TestCleanup]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ObjectFactory.Instance = null;
            this.target = new AXACalculateDeductiblePlugin();
            this.header = this.SetClaimHeader();
            this.ctheader = this.header.HistoricalClaimTransactionHeaders.FirstOrDefault();
            this.ctheader.Context = this.header.Context;
        }

        [TestMethod]
        [Ignore]
        public void CalculateDeductible_NoWarning_CustomBoolean04False()
        {
            ProcessInvocationPoint point = ProcessInvocationPoint.Virtual;
            ProcessResultsCollection result = target.ProcessComponent(this.ctheader, point, 0);
            Assert.AreEqual(this.ctheader.CustomBoolean04, false);
        }

        [TestMethod]
        [Ignore]
        public void CalculateDeductible_RecoveryReservesRequireManualReview_CustomBoolean04True()
        {
            this.ctheader.Context.Results.Add(ProcessInvocationPoint.Virtual, new ProcessResult(this.ctheader, string.Empty, ErrorSeverity.Warning, MessageConstants.RecoveryReservesRequireManualReview, "some message"));
            ProcessInvocationPoint point = ProcessInvocationPoint.Virtual;
            ProcessResultsCollection result = target.ProcessComponent(this.ctheader, point, 0);
            Assert.AreEqual(this.ctheader.CustomBoolean04, true);
        }

        [TestMethod]
        [Ignore]
        public void CalculateDeductible_ReservesRequireManualReview_CustomBoolean04True()
        {
            this.ctheader.Context.Results.Add(ProcessInvocationPoint.Virtual, new ProcessResult(this.ctheader, string.Empty, ErrorSeverity.Warning, MessageConstants.ReservesRequireManualReview, "some message"));
            ProcessInvocationPoint point = ProcessInvocationPoint.Virtual;
            ProcessResultsCollection result = target.ProcessComponent(this.ctheader, point, 0);
            Assert.AreEqual(this.ctheader.CustomBoolean04, true);
        }

        [TestMethod]
        [Ignore]
        public void CalculateDeductible_BothWarning_CustomBoolean04True()
        {
            this.ctheader.Context.Results.Add(ProcessInvocationPoint.Virtual, new ProcessResult(this.ctheader, string.Empty, ErrorSeverity.Warning, MessageConstants.RecoveryReservesRequireManualReview, "some message"));
            this.ctheader.Context.Results.Add(ProcessInvocationPoint.Virtual, new ProcessResult(this.ctheader, string.Empty, ErrorSeverity.Warning, MessageConstants.ReservesRequireManualReview, "some message"));
            ProcessInvocationPoint point = ProcessInvocationPoint.Virtual;
            ProcessResultsCollection result = target.ProcessComponent(this.ctheader, point, 0);
            Assert.AreEqual(this.ctheader.CustomBoolean04, true);
        }

        private ClaimHeader SetClaimHeader()
        {
            UnityContainer container = new UnityContainer();
            ClaimHeader ch = new ClaimHeader();
            ClaimsTransactionContext context = new ClaimsTransactionContext(Guid.NewGuid().ToString(), string.Empty, string.Empty);
            ch.Context = context;
            ch.ClaimProductVersionID = 1;

            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            ProductClaimTransaction productClaimTransaction = new ProductClaimTransaction() { ProductClaimTransactionID = 1 };
            metadataMock.Stub(m => m.GetProductClaimTransaction(Arg<long>.Is.Anything)).Return(productClaimTransaction);
            ObjectFactory.Instance = new ObjectFactory(container);

            ch.AddNewClaimTransactionHeader(new Xiap.Claims.Data.ClaimTransactionHeaderData()
            {
                ProductClaimTransactionID = 1,
                ClaimTransactionSource = (short)StaticValues.ClaimTransactionSource.Reserve,
                IsMultiClaimDetailTransaction = true,
                OriginalCurrencyCode = "GBP",
                AccountingCurrencyCode = "GBP",
                AccountingToBaseRate = 1,
                OriginalToBaseRate = 1,
                BaseCurrencyCode = "GBP"
            })
            .IsInProgress = false;

            return ch;
        }
    }
}
