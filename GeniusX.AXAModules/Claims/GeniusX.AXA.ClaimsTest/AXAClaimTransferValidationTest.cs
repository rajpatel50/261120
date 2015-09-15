using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xiap.Testing.Utils;
using Xiap.Claims.BusinessComponent;
using Xiap.Metadata.BusinessComponent;
using GeniusX.AXA.Claims.BusinessLogic;
using Rhino.Mocks;
using Xiap.Framework.Metadata;
using Microsoft.Practices.Unity;
using Xiap.Framework.Messages;
using System.Data.Objects;
using Xiap.Framework.Configuration;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using System.Data.Objects.DataClasses;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Data.Underwriting;
using System.Collections.Specialized;
using Xiap.Testing.Utils;
using Xiap.Framework.Caching;
using Xiap.Metadata.Data;

namespace GeniusX.AXA.ClaimsTest 
{
    //// <summary>
    //// Summary description for AXAClaimTransferValidationTest
    //// </summary>
    [TestClass]
    public class AXAClaimTransferValidationTest : BaseUnitTest
    {
        private const string ClaimRefernce01 = "#!001#";
        private const string ClaimRefernce02 = "#!002#";
        private const string ClaimRefernce03 = "#!003#";
        private const string ClaimRefernce04 = "#!004#";

        private TestContext testContextInstance;

        public AXAClaimTransferValidationTest()
        {
            // TODO: Add constructor logic here
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return this.testContextInstance;
            }

            set
            {
                this.testContextInstance = value;
            }
        }

        #region Additional test attributes
        // You can use the following additional attributes as you write your tests:
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        #endregion


       

        private ValueSetCacheEntry<ICodeValueData> GetValuesetCacheEntry(int setCode)
        {
            ValueSetCacheEntry<ICodeValueData> setCache = new ValueSetCacheEntry<ICodeValueData>();
            setCache.Values = new List<ICodeValueData>();
            ClaimHeaderStatusData typeData = new ClaimHeaderStatusData() { SetCode = setCode, Code = "CON" };
            setCache.Values.Add(typeData);

            Dictionary<string, ICodeValueNamesComponent> setNames = new Dictionary<string, ICodeValueNamesComponent>();

            ICodeValueNamesComponent typename = new ClaimHeaderStatusNames();
            typename.Code = "CON";
            typename.LanguageID = LanguageHelper.DefaultLanguageId;
            typename.LongDescription = "CON LD";
            typename.ShortDescription = "CON SD";
            setNames.Add("CON", typename);
            
            typename = new ClaimHeaderStatusNames();
            typename.Code = "CRO";
            typename.LanguageID = LanguageHelper.DefaultLanguageId;
            typename.LongDescription = "CRO LD";
            typename.ShortDescription = "CRO SD";
            setNames.Add("CRO", typename);

            typename = new ClaimHeaderStatusNames();
            typename.Code = "CRE";
            typename.LanguageID = LanguageHelper.DefaultLanguageId;
            typename.LongDescription = "CRE LD";
            typename.ShortDescription = "CRE SD";
            setNames.Add("CRE", typename);

            typename = new ClaimHeaderStatusNames();
            typename.Code = "CRL";
            typename.LanguageID = LanguageHelper.DefaultLanguageId;
            typename.LongDescription = "CRL LD";
            typename.ShortDescription = "CRL SD";
            setNames.Add("CRL", typename);

            setCache.ValueNames.Add(LanguageHelper.DefaultLanguageId, setNames);

            return setCache;
        }

        [TestInitialize]
        public void InitMock()
        {
            IClaimsEntities entities = MockRepository.GenerateStub<IClaimsEntities>();

            Dictionary<string, long> headerIds = new Dictionary<string, long>();
            headerIds.Add(ClaimRefernce01, 1);
            headerIds.Add(ClaimRefernce02, 2);
            headerIds.Add(ClaimRefernce03, 3);
            headerIds.Add(ClaimRefernce04, 4);

            Dictionary<long, Tuple<string, string, long?>> headerstatuses = new Dictionary<long, Tuple<string, string, long?>>();

            headerstatuses.Add(1, new Tuple<string, string, long?>(ClaimRefernce01, "CON", null));
            headerstatuses.Add(2, new Tuple<string, string, long?>(ClaimRefernce02, "CRO", null));
            headerstatuses.Add(3, new Tuple<string, string, long?>(ClaimRefernce03, "CRE", null));
            headerstatuses.Add(4, new Tuple<string, string, long?>(ClaimRefernce04, "CRL", null));

            entities.Stub(m => m.GetClaimHeaderIDs(new List<string>() { ClaimRefernce01 })).Return(headerIds);
            entities.Stub(m => m.GetHeaderStatusCodesByClaimHeaderIDs(new List<long?>() { 1 })).Return(headerstatuses);

            entities.Stub(m => m.GetClaimHeaderIDs(new List<string>() { ClaimRefernce02 })).Return(headerIds);
            entities.Stub(m => m.GetHeaderStatusCodesByClaimHeaderIDs(new List<long?>() { 2 })).Return(headerstatuses);

            entities.Stub(m => m.GetClaimHeaderIDs(new List<string>() { ClaimRefernce03 })).Return(headerIds);
            entities.Stub(m => m.GetHeaderStatusCodesByClaimHeaderIDs(new List<long?>() { 3 })).Return(headerstatuses);

            entities.Stub(m => m.GetClaimHeaderIDs(new List<string>() { ClaimRefernce04 })).Return(headerIds);
            entities.Stub(m => m.GetHeaderStatusCodesByClaimHeaderIDs(new List<long?>() { 4 })).Return(headerstatuses);

            IConfigurationManager icm = MockRepository.GenerateStub<IConfigurationManager>();
            NameValueCollection appsetting = new NameValueCollection();
            appsetting.Add(ClaimConstants.APP_SETTING_KEY_CLAIMTRANSFERINVALIDCLAIMHEADERSTATUSES, "CON, CRO, CRE, CRL");
            icm.Stub(c => c.AppSettings).Return(appsetting);

            

            ////var cache = MockRepository.GenerateStub<IXiapCache>();
            ////container.RegisterInstance<IXiapCache>(cache);
            ////CacheManager.SetCache(cache);

            CacheManager.AddToCache(string.Format("XIAP_SystemValueSetCache_{0}", (int)SystemValueSetCodeEnum.ClaimHeaderStatus), this.GetValuesetCacheEntry((int)SystemValueSetCodeEnum.ClaimHeaderStatus));
            SystemValueSetCache cacheQury = MockRepository.GenerateStub<SystemValueSetCache>();

            IMetadataEntities metadataMock = MockRepository.GenerateStub<IMetadataEntities>();
            UnityContainer container = new UnityContainer();
            container.RegisterType<IMessageService, Xiap.Testing.Utils.Mocks.MockMessagingService>();
            container.RegisterInstance<IClaimsEntities>(entities);
            container.RegisterInstance<IConfigurationManager>(icm);
            container.RegisterInstance<IClaimsEntities>("IClaimsEntities", entities);
            container.RegisterInstance<SystemValueSetCache>(cacheQury);
            container.RegisterInstance<IMetadataEntities>("IMetadataEntities", metadataMock);

            ObjectFactory.Instance = new ObjectFactory(container);
        }

        [TestMethod]
        [Ignore]
        public void ValidateClaimForTransfer_For_Invalid_ClaimReference()
        {
            AXAClaimTransferValidation claimValidation = new AXAClaimTransferValidation();
            TestMethod method = delegate { claimValidation.ValidateClaimForTransfer(ClaimRefernce01 + "INVALID", string.Empty); };
            ValidateException(typeof(ValidationException), MessageConstants.CLAIM_INVALID_CLAIMREFERENCE, method);
        }

        [TestMethod]
        [Ignore]
        public void ValidateClaimForTransfer_For_Invalid_HeaderStatusCodes()
        {
            AXAClaimTransferValidation claimValidation = new AXAClaimTransferValidation();
            TestMethod method = delegate { claimValidation.ValidateClaimForTransfer(ClaimRefernce01, string.Empty); };
            ValidateException(typeof(ValidationException), ClaimConstants.CLAIM_TRANSFER_INVALID_FOR_CLAIMHEADERSTATUSES, method);

            method = delegate { claimValidation.ValidateClaimForTransfer(ClaimRefernce02, string.Empty); };
            ValidateException(typeof(ValidationException), ClaimConstants.CLAIM_TRANSFER_INVALID_FOR_CLAIMHEADERSTATUSES, method);

            method = delegate { claimValidation.ValidateClaimForTransfer(ClaimRefernce03, string.Empty); };
            ValidateException(typeof(ValidationException), ClaimConstants.CLAIM_TRANSFER_INVALID_FOR_CLAIMHEADERSTATUSES, method);

            method = delegate { claimValidation.ValidateClaimForTransfer(ClaimRefernce04, string.Empty); };
            ValidateException(typeof(ValidationException), ClaimConstants.CLAIM_TRANSFER_INVALID_FOR_CLAIMHEADERSTATUSES, method);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CacheManager.RemoveFromCache(string.Format("XIAP_SystemValueSetCache_{0}", SystemValueSetCodeEnum.ClaimHeaderStatus));
            ObjectFactory.Instance = null;
        }
    }
}
