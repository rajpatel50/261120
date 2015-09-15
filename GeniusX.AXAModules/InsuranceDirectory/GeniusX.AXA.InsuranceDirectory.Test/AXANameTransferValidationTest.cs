using System;
using System.Data.Objects;
using System.Linq;
using GeniusX.AXA.InsuranceDirectory.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Configuration;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.Metadata;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.InsuranceDirectory.BusinessTransaction;
using Xiap.Metadata.BusinessComponent;

namespace GeniusX.AXA.InsuranceDirectory.Test
{
    [TestClass]
    public class AXANameTransferValidationTest
    {
        private AXANameTransferValidation component;
        private IUnityContainer container;
        private IDecisionTableHelper decisionTableHelper;
        private IDecisionTableComponent decisionTableComponent1;
        private IDecisionTableComponent decisionTableComponent2;
        private Name name;

        public static Name GetPersonwithUsages()
        {
            long id = 346;
            Name existingName;
            using (InsuranceDirectoryEntities entities = InsuranceDirectoryEntitiesFactory.GetInsuranceDirectoryEntities())
            {
                existingName = (from row in entities.Name
                                where row.NameID == id
                                select row).FirstOrDefault();
            }

            if (existingName == null)
            {
                IInsuranceDirectoryBusinessTransaction ap = InsuranceDirectoryBusinessTransactionFactory.CreateName(NameType.Person);
                PersonDetailVersion pdv = (ap.Component as Name).AddNewPersonDetailVersion();
                CreatePersonwithTwoUsage(ap);
                ap.Complete();


                int nameId = Convert.ToInt32(ap.Name.NameID);
                Xiap.InsuranceDirectory.BusinessTransaction.IInsuranceDirectoryBusinessTransaction businessTransaction = InsuranceDirectoryBusinessTransactionFactory.AmendName(nameId);

                AmendExistingName(businessTransaction);
                businessTransaction.Complete();

                return businessTransaction.Name;
            }
            else
            {
                return existingName;
            }
        }

        internal static void CreatePersonwithTwoUsage(IInsuranceDirectoryBusinessTransaction businessTransaction)
        {
            SetNameDetails(businessTransaction.Name);
            PersonDetailVersion Person = businessTransaction.Name.PersonDetailVersions.First();
            SetPersonDetails(Person);

            NameUsage nameUsage = businessTransaction.Name.AddNewNameUsage("CMH");
            NameUsage nameUsage2 = businessTransaction.Name.AddNewNameUsage("DRV");
        }

        internal static void AmendExistingName(Xiap.InsuranceDirectory.BusinessTransaction.IInsuranceDirectoryBusinessTransaction businessTransaction)
        {
            businessTransaction.Name.NameReference = "TESTABC140";
        }

        private static void SetNameDetails(Name name)
        {
            if (name.NameType == null)
            {
                name.NameType = (byte)NameType.Company;
                name.NameReference = "Test";
            }
        }

        private static void SetPersonDetails(PersonDetailVersion Person)
        {
            Person.ListName = "LISTNAME";
            Person.Surname = "XCHANGING";
            Person.Forename = "ANKIT";
            Person.ContactNumber01 = "1234567890";
            Person.ContactNumber02 = "1234567890";
            Person.ContactNumber03 = "1234567890";
            Person.ContactNumber04 = "1234567890";
            Person.ContactNumber05 = "1234567890";
            Person.Email01 = "email01@xchanging.com";
            Person.Email02 = "email02@xchanging.com";
            Person.Email03 = "email03@xchanging.com";
            Person.Gender = 1;
            ////Person.VersionStartDate = DateTime.Now;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // target = new AXANameTransferValidation();
            this.component = new AXANameTransferValidation();


            this.container = new UnityContainer();
            ObjectFactory.Instance = new ObjectFactory(this.container);

            IComponentMetadata metadata = MockRepository.GenerateStub<IComponentMetadata>();

            this.decisionTableHelper = MockRepository.GenerateStub<IDecisionTableHelper>();
            this.decisionTableComponent1 = MockRepository.GenerateStub<IDecisionTableComponent>();
            this.decisionTableComponent2 = MockRepository.GenerateStub<IDecisionTableComponent>();

            this.container.RegisterInstance<IDecisionTableHelper>(this.decisionTableHelper);
            this.container.RegisterInstance<IComponentMetadata>(metadata);
            var metadataMock = MockRepository.GenerateStub<IMetadataQuery>();
            this.container.RegisterInstance<IMetadataQuery>(metadataMock);

            var validation = MockRepository.GenerateStub<ICopyValidation>();
            this.container.RegisterInstance<ICopyValidation>(validation);

            NameUsageType nst = new NameUsageType() { Code = "CMH" };
            metadata.Stub(m => m.GetDefinitionComponent<NameUsageType>()).Return(nst);
            // this.container.RegisterInstance<IDecisionTableComponent>(this.decisionTableComponent1);
            // this.container.RegisterInstance<IDecisionTableComponent>(this.decisionTableComponent2);
            this.container.RegisterType<ObjectContext, MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            this.container.RegisterType<MetadataEntities>("MetadataEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
            IConfigurationManager configurationManager = MockRepository.GenerateStub<IConfigurationManager>();
            configurationManager.Stub(m => m.AppSettings).Return(new System.Collections.Specialized.NameValueCollection());
            this.container.RegisterInstance<IConfigurationManager>(configurationManager);
            this.container.RegisterType<ObjectContext, InsuranceDirectoryEntities>("InsuranceDirectoryEntities", new InjectionConstructor[] { new InjectionConstructor(new object[] { }) });
        }


        [TestCleanup]
        public void CleanUp()
        {
            ObjectFactory.Instance = null;
        }

        [TestMethod]
        [Ignore]
        public void NameTransfer_Validation_Valid()
        {
            this.name = GetPersonwithUsages();

            object[] condition1 = new object[2];
            condition1[0] = "CMH";
            condition1[1] = "VALID";

            object[] condition2 = new object[2];
            condition1[0] = "DRV";
            condition1[1] = "VALID";


            this.decisionTableComponent1.Action1 = true;
            this.decisionTableComponent1.IsValid = true;

            this.decisionTableComponent2.Action1 = true;
            this.decisionTableComponent2.IsValid = true;

            this.decisionTableHelper.Stub(s => s.TryCall("X-NTNU", System.DateTime.Now.Date, out this.decisionTableComponent1, condition1)).OutRef(this.decisionTableComponent1).IgnoreArguments().Return(true);
            this.decisionTableHelper.Stub(s => s.TryCall("X-NTNU", System.DateTime.Now.Date, out this.decisionTableComponent2, condition2)).OutRef(this.decisionTableComponent2).IgnoreArguments().Return(true);

            Assert.IsTrue(this.component.ValidateNameForTransfer(this.name.NameReference));
        }

        [TestMethod]
        [Ignore]
        public void NameTransfer_Validation_NotValid()
        {
            this.name = GetPersonwithUsages();
            
            object[] condition1 = new object[2];
            condition1[0] = "CMH";
            condition1[1] = "VALID";

            object[] condition2 = new object[2];
            condition1[0] = "DRV";
            condition1[1] = "VALID";


            this.decisionTableComponent1.Action1 = true;
            this.decisionTableComponent1.IsValid = true;

            this.decisionTableComponent2.Action1 = false;
            this.decisionTableComponent2.IsValid = true;

            this.decisionTableHelper.Stub(s => s.TryCall("X-NTNU", System.DateTime.Now.Date, out this.decisionTableComponent2, condition2)).OutRef(this.decisionTableComponent2).IgnoreArguments().Return(false);
            this.decisionTableHelper.Stub(s => s.TryCall("X-NTNU", System.DateTime.Now.Date, out this.decisionTableComponent1, condition1)).OutRef(this.decisionTableComponent1).IgnoreArguments().Return(true);
            
            Assert.IsFalse(this.component.ValidateNameForTransfer(this.name.NameReference));
        }
    }
}
