using System.Data.Objects;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class AXAClaimTransactionHeaderDefaulterTest : ComponentPluginBaseTest<AXAClaimTransactionHeaderDefaulter>
    {
        protected const long VALID_CLAIM_PRODUCT_VERSIONID = 1060;

        [TestMethod]
        public void ClaimTransactionheaderDefaulter_HistoricalClaimTransction()
        {
            AXAClaimTransactionHeaderDefaulter target = new AXAClaimTransactionHeaderDefaulter();
            ClaimHeader header = this.SetClaimHeader();
            ClaimTransactionHeader ctheader = header.HistoricalClaimTransactionHeaders.FirstOrDefault();
            Assert.IsNull(ctheader.ClaimTransactionHeaderReference);
            ProcessInvocationPoint point = ProcessInvocationPoint.Virtual;
            ProcessResultsCollection result = target.ProcessComponent(ctheader, point, 0);
            Assert.IsNotNull(ctheader.ClaimTransactionHeaderReference);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }

        private ClaimHeader SetClaimHeader()
        {
            UnityContainer container = new UnityContainer();
            ClaimHeader ch = new ClaimHeader();
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
                ClaimTransactionSource = (short)StaticValues.ClaimTransactionSource.Payment,
                IsMultiClaimDetailTransaction = true,
                OriginalCurrencyCode = "GBP",
                AccountingCurrencyCode = "GBP",
                AccountingToBaseRate = 1,
                OriginalToBaseRate = 1,
                BaseCurrencyCode = "GBP"
            })
            .IsInProgress = false;

            ch.AddNewClaimTransactionHeader(new Xiap.Claims.Data.ClaimTransactionHeaderData()
            {
                ProductClaimTransactionID = 1,
                ClaimTransactionSource = (short)StaticValues.ClaimTransactionSource.Payment,
                IsMultiClaimDetailTransaction = true,
                OriginalCurrencyCode = "GBP",
                AccountingCurrencyCode = "GBP",
                AccountingToBaseRate = 1,
                OriginalToBaseRate = 1,
                BaseCurrencyCode = "GBP"
            })
            .IsInProgress = true;
            
            return ch;
        }
    }
}
