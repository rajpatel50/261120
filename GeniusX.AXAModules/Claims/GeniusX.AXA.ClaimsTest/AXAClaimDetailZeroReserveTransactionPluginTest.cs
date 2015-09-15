using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xiap.Framework;
using GeniusX.AXA.Claims.BusinessLogic;
using Xiap.Metadata.Data.Enums;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.Extensions;
using Xiap.Testing.Utils;
using Xiap.Claims.Test;
using Microsoft.Practices.Unity;
using Xiap.Framework.Caching;
using Rhino.Mocks;
using Xiap.Framework.Common.Product;
using Xiap.Metadata.BusinessComponent;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class AXAClaimDetailZeroReserveTransactionPluginTest
    {
        private static Builder<ClaimDetail> claimDetailRef;
        private Xiap.Testing.Utils.Mocks.MockBusinessTransaction mockBT = null;
        private ClaimHeader claimHeader = null;
       
        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }

        [TestInitialize]
        public void Init()
        {
            UnityContainer container = new UnityContainer();
            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            container.RegisterInstance<MetadataEntities>("MetadataEntities", new MetadataEntities());
            IProductClaimDetailQuery claimDetailQuery = MockRepository.GenerateStub<IProductClaimDetailQuery>();
            container.RegisterInstance<IProductClaimDetailQuery>(claimDetailQuery);
            IMetadataQuery metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            container.RegisterInstance<IMetadataQuery>(metadataMock);
            ObjectFactory.Instance = new ObjectFactory(container);
            this.mockBT = GetMockClaimsBusinessTransaction("AmendClaim", null);

            this.claimHeader = new BusinessComponentBuilder<ClaimHeader>()
                  .SetProperty(c => c.ClaimProductVersionID = 1)
                  .Add(new BusinessComponentBuilder<ClaimDetail>()
                    .As(out claimDetailRef)
                    .SetProperty(a => a.ClaimDetailStatusCode = "TST"))
                  .Add(new BusinessComponentBuilder<ClaimTransactionHeader>()
                      .SetProperty(a => a.IsInProgress = true)
                      .SetProperty(a => a.ClaimTransactionSource = 1)
                    .Add(new BusinessComponentBuilder<ClaimTransactionGroup>()
                        .AddRef(claimDetailRef)))
                  .Build();

            ClaimDetail claimDetail = this.claimHeader.ClaimDetails.First();
            string status = claimDetail.OriginalTransitionStatus;
            claimDetail.ClaimDetailStatusCode = "TST1";
            claimDetail.ClaimDetailInternalStatus = (short)StaticValues.ClaimDetailInternalStatus.Finalized;
            this.mockBT.Component = this.claimHeader as IBusinessComponent;
            
            ProductXML.ProductClaimDetail productClaimDetail = new ProductXML.ProductClaimDetail() { ProductClaimDetailID = 1, IsFinalizationWithNonZeroOSAllowed = true, ClaimDetailTypeCode = "TEST" };
            productClaimDetail.ProductClaimDetailToComponentLinks = new ProductXML.ProductClaimDetailToComponentLink[1]
                                                        {
                                                            new ProductXML.ProductClaimDetailToComponentLink()                                                            
                                                        };

            claimDetailQuery.Stub(x => x.GetProductClaimDetail(Arg<long>.Is.Anything)).Return(productClaimDetail);
           
            metadataMock.Stub(c => c.GetClaimDetailStatusCodeStatusLevel(Arg<long>.Is.Anything, Arg<string>.Is.Anything)).Return(10);
        }

        [TestMethod]
        public void ProcessComponent_CloseClaimDetail_ZeroReserveOnCloseClaim()
        {
            AXAClaimDetailZeroReserveTransactionPlugin plugin = new AXAClaimDetailZeroReserveTransactionPlugin();
            plugin.ProcessTransaction(this.mockBT, TransactionInvocationPoint.PreComplete, 1, null);
            this.claimHeader.InProgressClaimTransactionHeaders.ForEach(cth =>
                {
                    Assert.AreEqual("Claim Detail Closed", cth.ClaimTransactionDescription);
                    Assert.IsNotNull(cth.ClaimTransactionHeaderReference);
                });
        }

        private static Xiap.Testing.Utils.Mocks.MockBusinessTransaction GetMockClaimsBusinessTransaction(string transactionType, string[] parameters)
        {
            Xiap.Testing.Utils.Mocks.MockBusinessTransaction mock = new Xiap.Testing.Utils.Mocks.MockBusinessTransaction(transactionType, parameters, "Claims", new ClaimsTransactionContext(Guid.NewGuid().ToString(), "Claims", transactionType));
            return mock;
        }
    }
}
