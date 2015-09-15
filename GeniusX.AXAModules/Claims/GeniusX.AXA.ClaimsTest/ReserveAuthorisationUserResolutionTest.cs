using System.Collections.Generic;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class ReserveAuthorisationUserResolutionTest
    {
        private const long TransactionHeaderID = 1;
        private const string RefereeIdentity = "RefereeIdentity";
        private User currentUser, supervisor;
        private ClaimTransactionHeader header;
        private ReserveAuthorisationUserResolution userResolver = new ReserveAuthorisationUserResolution();


        [TestInitialize]
        public void Initialize()
        {
            var container = new UnityContainer();
            ObjectFactory.Instance = new ObjectFactory(container);

            this.currentUser = new User { UserIdentity = "currentUserIdentity", UserID = 1 };
            this.supervisor = new User { UserIdentity = "manager", UserID = 2 };
            this.currentUser.ManagerID = this.supervisor.UserID;

            this.header = new ClaimTransactionHeader { CustomReference01 = RefereeIdentity };

            var claimsEntities = MockRepository.GenerateStub<IClaimsQuery>();
            claimsEntities.Stub(a => a.GetClaimTransactionHeader(TransactionHeaderID)).IgnoreArguments().Return(this.header);
            container.RegisterInstance<IClaimsQuery>(claimsEntities);

            var metadataEntities = MockRepository.GenerateStub<IMetadataQuery>();
            metadataEntities.Stub(a => a.GetUserByUserIdentity(this.currentUser.UserIdentity)).Return(this.currentUser);
            metadataEntities.Stub(a => a.GetUserByUserId(this.supervisor.UserID)).Return(this.supervisor);
            container.RegisterInstance(metadataEntities);
        }

        [TestMethod]
        public void GetData_RefereeAvailable_RefereeIndentityReturned()
        {
            var destination = this.userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", TransactionHeaderID }, { "creatorUserIdentity", this.currentUser.UserIdentity } });
            Assert.AreEqual(RefereeIdentity, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.User.ToString(), destination.GetAttribute("DestinationType"));
        }

        [TestMethod]
        public void GetData_NoReferee_SupervisorIdentityReturned()
        {
            this.header.CustomReference01 = null;

            var destination = this.userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", TransactionHeaderID }, { "creatorUserIdentity", this.currentUser.UserIdentity } });
            Assert.AreEqual(this.supervisor.UserIdentity, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.User.ToString(), destination.GetAttribute("DestinationType"));
        }

        [TestMethod]
        public void GetData_NoReferee_NoSupervisor_BSupportIdentityReturned()
        {
            this.header.CustomReference01 = null;
            this.currentUser.ManagerID = null;
            var destination = this.userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", TransactionHeaderID }, { "creatorUserIdentity", this.currentUser.UserIdentity } });
            Assert.AreEqual("BSupport", destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.Role.ToString(), destination.GetAttribute("DestinationType"));
            
            // assigning Manager ID back, if it is used in further tests.
            this.currentUser.ManagerID = this.supervisor.UserID;
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
