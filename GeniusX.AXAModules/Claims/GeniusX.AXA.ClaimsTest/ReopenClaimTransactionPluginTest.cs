using System;
using System.Data.Objects;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Rhino.Mocks;
using Xiap.Framework.Configuration;
using Xiap.Framework.Messages;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ReopenClaimTransactionPluginTest
    {
        private ProcessResultsCollection results;
        private ClaimHeader claimHeader = null;
        protected const long AXA_VALID_CLAIM_PRODUCT_VERSIONID = 3;
        public const string CREATECLAIM = "CreateClaim";
        
        [TestInitialize]
        public void TestInitialize()
        {
            this.claimHeader = new ClaimHeader() { ClaimStage = (short)StaticValues.ClaimStage.Claim };
            UnityContainer container = new UnityContainer();
            ObjectFactory.Instance = new ObjectFactory(container);

            container.RegisterType<IMessageService, MessageService>();
            IConfigurationManager icm = MockRepository.GenerateStub<IConfigurationManager>();
            icm.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(icm);

            var metadataEntities = MockRepository.GenerateStub<IMetadataQuery>();
            container.RegisterInstance(metadataEntities);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
        }
 
        [TestMethod]
        public void ValidateMigrationStatus_MigrationStatusIsNull_ClaimReopenNotAllowed()
        {
            string[] parameters = { this.claimHeader.ClaimHeaderID.ToString() };
            Xiap.Testing.Utils.Mocks.MockBusinessTransaction mockBT = new Xiap.Testing.Utils.Mocks.MockBusinessTransaction("ReopenClaim", parameters, "Claims", new ClaimsTransactionContext(Guid.NewGuid().ToString(), "Claims", "ReopenClaim"));
            this.claimHeader.ClaimHeaderID = 1;
            this.claimHeader.CustomCode18 = null;
            this.claimHeader.CustomCode19 = "C";
            mockBT.Component = this.claimHeader;
            ReopenClaimTransactionPlugin transaction = new ReopenClaimTransactionPlugin();
            this.results = transaction.ProcessTransaction(mockBT, TransactionInvocationPoint.PreCreate, 1, parameters);

            Assert.AreEqual(1, this.results.Count);
            Assert.AreEqual("REOPENING_OF_UNPROCESSED_CLAIM_NOT_ALLOWED", this.results.Results.First().MessageId);
        }

        [TestMethod]
        public void ClaimReopenNotAllowedErrorTest()
        {
            string[] parameters = { this.claimHeader.ClaimHeaderID.ToString() };
            Xiap.Testing.Utils.Mocks.MockBusinessTransaction mockBT = new Xiap.Testing.Utils.Mocks.MockBusinessTransaction("ReopenClaim", parameters, "Claims", new ClaimsTransactionContext(Guid.NewGuid().ToString(), "Claims", "ReopenClaim"));
            this.claimHeader.ClaimHeaderID = 1;
            this.claimHeader.CustomCode18 = "F01"; // ClaimWakeUp Validation error check
            this.claimHeader.CustomCode19 = "C";
            mockBT.Component = this.claimHeader;
            ReopenClaimTransactionPlugin transaction = new ReopenClaimTransactionPlugin();
            this.results = transaction.ProcessTransaction(mockBT, TransactionInvocationPoint.PreCreate, 1, parameters);

            Assert.AreEqual(1, this.results.Count);
            Assert.AreEqual(ClaimConstants.REOPENING_OF_CLAIM_NOT_ALLOWED_WITHOUT_POLICY, this.results.Results.First().MessageId);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
