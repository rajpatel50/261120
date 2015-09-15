using System;
using System.Data.Objects;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Configuration;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Testing.Utils;
using Xiap.Claims.Test;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ClaimTransactionStatusChangePluginTest
    {
        private static Builder<ClaimDetail> claimDetailRefFirst;
        private static Builder<ClaimDetail> claimDetailRefLast;
        private UnityContainer container;
        private ClaimHeader claimHeader;
        private Xiap.Testing.Utils.Mocks.MockBusinessTransaction mockBT;


        [TestInitialize]
        public void Initialise()
        {
            this.container = new UnityContainer();
            ObjectFactory.Instance = new ObjectFactory(this.container);

            this.container.RegisterType<IMessageService, MessageService>();
            IConfigurationManager icm = MockRepository.GenerateStub<IConfigurationManager>();
            icm.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            this.container.RegisterInstance<IConfigurationManager>(icm);

            var metadataEntities = MockRepository.GenerateStub<IMetadataQuery>();
            this.container.RegisterInstance(metadataEntities);
            this.container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            this.container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });

            string[] parameters = { "1" };
            this.mockBT = GetMockClaimsBusinessTransaction("CreateClaim", parameters);

            this.claimHeader = new BusinessComponentBuilder<ClaimHeader>()
                    .Add(new BusinessComponentBuilder<ClaimDetail>()
                        .As(out claimDetailRefFirst))
                    .Add(new BusinessComponentBuilder<ClaimDetail>()
                        .As(out claimDetailRefLast))
                    .Add(new BusinessComponentBuilder<ClaimTransactionHeader>()
                      .SetProperty(a => a.IsInProgress = true)
                      .SetProperty(a => a.ClaimTransactionSource = 1)
                    .Add(new BusinessComponentBuilder<ClaimTransactionGroup>()
                        .AddRef(claimDetailRefFirst))
                    .Add(new BusinessComponentBuilder<ClaimTransactionGroup>()
                        .AddRef(claimDetailRefLast)))
                  .Build();
            this.mockBT.Component = this.claimHeader;
        }

        [TestMethod]
        public void ReserveWithUnconfirmedStatusTest()
        {
            this.claimHeader.ClaimHeaderStatusCode = "COU";
            ClaimTransactionStatusChangePlugin plugin = new ClaimTransactionStatusChangePlugin();
            plugin.ProcessTransaction(this.mockBT, TransactionInvocationPoint.PreComplete, 1, null);
            Assert.AreEqual(this.claimHeader.ClaimHeaderStatusCode, "CES");  
        }


        [TestMethod]
        public void ReserveWithReportStatusTest()
        {
            this.claimHeader.ClaimHeaderStatusCode = "CRO";
            ClaimTransactionStatusChangePlugin plugin = new ClaimTransactionStatusChangePlugin();
            plugin.ProcessTransaction(this.mockBT, TransactionInvocationPoint.PreComplete, 1, null);
            Assert.AreEqual(this.claimHeader.ClaimHeaderStatusCode, "CRE");
        }

        [TestMethod]
        public void PaymentEntryNotAllowedForReportsOnlyClaimTest()
        {
            this.claimHeader.ClaimHeaderStatusCode = "CRE";
            this.mockBT.Component = this.claimHeader;
            this.claimHeader.InProgressClaimTransactionHeaders.First().ClaimTransactionSource = 2; // Payment
            ClaimTransactionStatusChangePlugin plugin = new ClaimTransactionStatusChangePlugin();
            ProcessResultsCollection results = plugin.ProcessTransaction(this.mockBT, TransactionInvocationPoint.PreComplete, 1, null);
            Assert.AreEqual(this.claimHeader.ClaimHeaderStatusCode, "CRE");
            Assert.AreEqual(results.Results.First().MessageId, "ENTRY_NOT_ALLOWED_FOR_REPORT_ONLY_CLAIMS");
        }

        [TestMethod]
        public void RecoveryReceiptEntryNotAllowedForReportsOnlyClaimTest()
        {
            this.claimHeader.ClaimHeaderStatusCode = "CRE";
            this.mockBT.Component = this.claimHeader;
            this.claimHeader.InProgressClaimTransactionHeaders.First().ClaimTransactionSource = 4; // RecoveryReceipt
            ClaimTransactionStatusChangePlugin plugin = new ClaimTransactionStatusChangePlugin();
            ProcessResultsCollection results = plugin.ProcessTransaction(this.mockBT, TransactionInvocationPoint.PreComplete, 1, null);
            Assert.AreEqual(this.claimHeader.ClaimHeaderStatusCode, "CRE");
            Assert.AreEqual(results.Results.First().MessageId, "ENTRY_NOT_ALLOWED_FOR_REPORT_ONLY_CLAIMS");
        }

        private static Xiap.Testing.Utils.Mocks.MockBusinessTransaction GetMockClaimsBusinessTransaction(string transactionType, string[] parameters)
        {
            Xiap.Testing.Utils.Mocks.MockBusinessTransaction mock = new Xiap.Testing.Utils.Mocks.MockBusinessTransaction(transactionType, parameters, "Claims", new ClaimsTransactionContext(Guid.NewGuid().ToString(), "Claims", transactionType));
            return mock;
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
