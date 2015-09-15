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
using Xiap.Framework.Entity;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Security;
using FrameworkSecurity = Xiap.Framework.Security;

namespace GeniusX.AXA.ClaimsTest
{
    /// <summary>
    /// This is a test class for ClaimsBusinessLogicHelperTest and is intended
    /// to contain all ClaimsBusinessLogicHelperTest Unit Tests
    /// </summary>
    [TestClass()]
    public class ClaimsBusinessLogicHelperTest
    {
        private ClaimEvent component;
        private ProductEvent productEvent;

        internal static void TestInitialize(ref ClaimEvent component, ref ProductEvent pce)
        {
            component = new ClaimEvent();
            component.ProductEventID = 34;

            if (pce == null)
            {
                pce = new ProductEvent();
            }

            component.InternalClaimHeader = new ClaimHeader();
            component.ClaimHeader.InternalClaimDetails.Add(new ClaimDetail() { ClaimDetailReference = "D0001" });
            ClaimInvolvement claimInvolvement = new ClaimInvolvement() { InternalClaimHeader = component.InternalClaimHeader, ClaimInvolvementType = (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.NameInvolvement, InternalClaimNameInvolvements = new System.Data.Objects.DataClasses.EntityCollection<ClaimNameInvolvement>() };
            ClaimNameInvolvement nameInvolvement = new ClaimNameInvolvement() { NameInvolvementType = (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MainClaimHandler, NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest, NameID = 1 };
            claimInvolvement.InternalClaimNameInvolvements.Add(nameInvolvement);
            component.InternalClaimHeader.InternalClaimInvolvements.Add(claimInvolvement);

            pce.ProductVersion = new ProductVersion();
            pce.ProductVersion.Product = new Product();
            pce.ProductVersion.Product.Code = "TST";

            pce.EventType = new EventType();
            pce.EventType.IsStatusChangeEvent = false;
            pce.EventType.IsSystemCreationAllowed = false;
            pce.EventType.IsManualCreationAllowed = true;
            pce.EventType.IsManualDeletionAllowed = true;
            pce.EventType.Code = "ABCD";
            pce.EventTypeReference.SetEntityKey(new System.Data.EntityKey("MetadataEntities.EventType", "Code", "ABCD"));

            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();
            metadata.Stub(m => m.GetDefinitionComponent<EventType>()).Return(pce.EventType);

            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IComponentMetadata>(metadata);

            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            var list = new List<ProductEvent>() { pce };
            long productEventID = 34;

            metadataMock.Stub(m => m.GetProductEvent(productEventID));

            var listEventType = new List<EventType>() { pce.EventType };
            metadataMock.Stub(m => m.GetEventType("ABCD")).Return(listEventType.AsQueryable());

            EventTypeVersion etv = new EventTypeVersion();
            etv.EventTypeVersionID = 34;
            var listTypeVersion = new List<EventTypeVersion>() { etv };
            metadataMock.Stub(m => m.GetEventTypeVersion("ABCD")).Return(listTypeVersion.AsQueryable());

         

            container.RegisterInstance<IMetadataQuery>(metadataMock);
            container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            container.RegisterInstance<IConfigurationManager>(configurationManager);
            ObjectFactory.Instance = new ObjectFactory(container);

            component.Context = new ClaimsTransactionContext(string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// A test for GenerateProcessData
        /// </summary>
        [TestMethod()]
        public void GenerateProcessDataTest()
        {
            ClaimsBusinessLogicHelperTest.TestInitialize(ref this.component, ref this.productEvent);
            this.component.InternalClaimHeader.ClaimReference = "TestReference";
            this.component.TaskInitialDueDate = DateTime.Now.Date;
            PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(ProcessInvocationPoint.Created, this.component, new ProcessResultsCollection());
            Dictionary<string, object> actual = null; // TODO: Initialize to an appropriate value
            Dictionary<string, object> expected = new Dictionary<string, object>();
            expected.Add(ClaimsProcessHelper.CLAIMHEADERREFERENCE, "TestReference");
            expected.Add(ClaimsProcessHelper.TASKUSER, "TestUser");
            expected.Add(ClaimsProcessHelper.CLAIM_REVIEW_TASKDUEDATE, DateTime.Now.Date);
            expected.Add(ClaimsProcessHelper.CLAIMACTION, string.Empty);
            actual = ClaimsProcessHelper.GenerateProcessData(this.component.InternalClaimHeader, pluginHelper);
            Assert.IsTrue(actual.Count() > 1);
        }

        /// <summary>
        /// A test for GenrateFolio
        /// </summary>
        [TestMethod()]
        public void GenrateFolioTest()
        {
            ClaimHeader header = new ClaimHeader();
            header.ClaimReference = "TestReference";
            string expected = "TestReference";
            string actual;
            actual = ClaimsProcessHelper.GenerateFolio(header, ClaimsBusinessLogicHelper.GetInsuredFromHeader(header));
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for GetClaimProcessName
        /// </summary>
        [TestMethod()]
        public void GetClaimProcessNameTest()
        {
            string alias = "AXANewReOpenClaimProcessHandler";
            string expected = @"GeniusX.AXA.Workflows\NewReOpenclaim";
            string actual;
            actual = ClaimsProcessHelper.GetClaimProcessName(alias);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for GetMainClaimHandlerUserID
        /// </summary>
        [TestMethod()]
        [Ignore]
        public void GetMainClaimHandlerUserIDTest()
        {
            ClaimsBusinessLogicHelperTest.TestInitialize(ref this.component, ref this.productEvent);
            PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(ProcessInvocationPoint.Created, this.component, new ProcessResultsCollection());
            Nullable<long> expected = 1;
            Nullable<long> actual;
            actual = ClaimsBusinessLogicHelper.GetMainClaimHandlerUserID(pluginHelper);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for GetProcessDataString
        /// </summary>
        [TestMethod()]
        public void GetProcessDataStringTest()
        {
            Dictionary<string, object> processData = new Dictionary<string, object>();
            processData.Add("K1", "V1");
            processData.Add("K2", "V2");
            string expected = "K1:V1,K2:V2,";
            string actual = string.Empty;
            actual = ClaimsProcessHelper.GetProcessDataString(processData);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for TryGetUserIdentityName
        /// </summary>
        [TestMethod()]
        public void TryGetUserIdentityNameTest()
        {
            ClaimsBusinessLogicHelperTest.TestInitialize(ref this.component, ref this.productEvent);
            PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(ProcessInvocationPoint.Created, this.component, new ProcessResultsCollection());

            var currentuser = XiapSecurity.GetUser();
            string userIdentity = string.Empty;
            string userIdentityExpected = currentuser.UserIdentity;
            long nameID = currentuser.NameID;
            bool expected = true;
            
            if (nameID == 0)
            {
                expected = false;
                userIdentityExpected = string.Empty;
            }
            
            bool actual;
            actual = ClaimsBusinessLogicHelper.TryGetUserIdentityByName(nameID, out userIdentity);
            Assert.AreEqual(userIdentityExpected, userIdentity);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for TryGetUserIdentityName
        /// </summary>
        [TestMethod()]
        [Ignore]
        public void TryGetUserIdentityNameTest1()
        {
            long nameID = 1;
            ClaimsBusinessLogicHelperTest.TestInitialize(ref this.component, ref this.productEvent);
            PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(ProcessInvocationPoint.Created, this.component, new ProcessResultsCollection());
            string userIdentity = string.Empty;
            string userIdentityExpected = "TestUser";
            Nullable<long> userID;
            Nullable<long> userIDExpected = 1;
            bool expected = true;
            bool actual;
            actual = ClaimsBusinessLogicHelper.TryGetUserIdentityByName(nameID, out userIdentity, out userID);
            Assert.AreEqual(userIdentityExpected, userIdentity);
            Assert.AreEqual(userIDExpected, userID);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for TryGetUserIdentityByUser
        /// </summary>
        [TestMethod()]
        public void TryGetUserIdentityByUser()
        {
            ClaimsBusinessLogicHelperTest.TestInitialize(ref this.component, ref this.productEvent);
            var currentuser = XiapSecurity.GetUser();
            string userIdentity = string.Empty;
            string userIdentityExpected = currentuser.UserIdentity;
            long userID = currentuser.UserID;
            FrameworkSecurity.User user;
            bool expected = true;
            bool actual;
            actual = ClaimsBusinessLogicHelper.TryGetUserIdentityByUser(userID, out user);
            Assert.AreEqual(userIdentityExpected, user.UserIdentity);
            Assert.AreEqual(expected, actual);
        }
	
	/// <summary>
        /// A test for true CheckValidHeaderStatus
        /// </summary>
        [TestMethod]
        public void CheckValidHeaderStatusTrueTest()
        {
            ClaimHeader header = new ClaimHeader();
            header.ClaimHeaderStatusCode = "COU";
            Assert.IsTrue(ClaimsBusinessLogicHelper.CheckValidHeaderStatus(header, ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(ClaimConstants.APP_SETTING_KEY_HEADERSTATUSESFORINVALIDCLAIMTRANSFER)));
        }

	/// <summary>
        /// A test for False CheckValidHeaderStatus
        /// </summary>
        [TestMethod]
        public void CheckValidHeaderStatusFalseTest()
        {
            ClaimHeader header = new ClaimHeader();
            header.ClaimHeaderStatusCode = "CON";
            Assert.IsFalse(ClaimsBusinessLogicHelper.CheckValidHeaderStatus(header, ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(ClaimConstants.APP_SETTING_KEY_HEADERSTATUSESFORINVALIDCLAIMTRANSFER)));
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
