using System;
using System.Collections.Generic;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic.AuthorityCheck;
using Xiap.Claims.BusinessLogic.AuthorityCheck.Calculation;
using Xiap.Framework;
using Xiap.Framework.Data.Tasks;
using Xiap.Framework.Metadata;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.Claims.Test;
using CheckType = Xiap.Metadata.Data.Enums.StaticValues.ClaimFinancialAuthorityCheckType;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class PaymentAuthorisationUserResolutionTest
    {
        private const string GradeCode1 = "GradeCode1";
        private const string GradeCode2 = "GradeCode2";
        private const string GradeStructureCode = ClaimConstants.PRODUCT_MOTOR_GRADESTRUCTURETYPE_CLAIMDETAIL_NOTTPI;
        private const string GradeStructureCode1 = ClaimConstants.PRODUCT_MOTOR_GRADESTRUCTURETYPE_CLAIMDETAIL_TPI;
        private static Builder<ClaimDetail> claimDetailRef;
        private User currentUser, targetUser1, targetUser2;
        private PaymentAuthorisationUserResolution _userResolver = new PaymentAuthorisationUserResolution();
        
        private IMetadataQuery metadataEntities;
        private ProductClaimDefinition productClaimDefinition;
        private string userGradeCode;
        private string businessSupportRole;
        private List<Grade> gradesforStructure;
        private List<User> usersForGradeCode1, usersForGradeCode2;
        private List<IEventDestination> eventDestinations;
        private ClaimHeader claimHeader;

        [TestInitialize]
        public void Initialise()
        {
            var container = new UnityContainer();

            this.RegisterAuthorityChecks(container);

            var claimTransaction = new BusinessComponentBuilder<ClaimTransactionHeader>();

            this.currentUser = CreateUser("CurrenUserIdentity", 1, 10);
            this.targetUser1 = CreateUser("TargetUser1Identity", 2, 10);
            this.targetUser2 = CreateUser("TargetUser2Identity", 3, 0);

            var componentMetadata = MockRepository.GenerateStub<IComponentMetadata>();
            container.RegisterInstance<IComponentMetadata>(componentMetadata);
            var product = new ProductBuilder<ProductVersion>(componentMetadata)
                            .SetProperty(a => a.GradeStructureType = GradeStructureCode) 
                            .Add(new ProductBuilder<ProductClaimDefinition>(componentMetadata)
                                .SetProperty(a => a.IsManualAuthorisationAlwaysAllowedIfNoChecksAreActive = true))
                         .Build();

            this.productClaimDefinition = product.ProductClaimDefinition;

            ObjectFactory.Instance = new ObjectFactory(container);

            var clmHeader = new BusinessComponentBuilder<ClaimHeader>()
                      .Add(new BusinessComponentBuilder<ClaimDetail>()
                      .As(out claimDetailRef)
                       .SetProperty(a => a.PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Header)
                       .SetProperty(a => a.ProductClaimDetailID = 1)
                       .SetProperty(a => a.ClaimDetailReference = "Claim Detail Reference"))
                      .Add(new BusinessComponentBuilder<ClaimTransactionHeader>()
                        .SetProperty(a => a.IsInProgress = true)
                        .Add(new BusinessComponentBuilder<ClaimTransactionGroup>()
                            .AddRef(claimDetailRef)
                            .SetProperty(a => a.AdministerClaimMethod = 2)))
                  .Build();

            clmHeader.ClaimHeaderAnalysisCode01 = ClaimConstants.CH_ANALYSISCODE_MOTOR;

            this.claimHeader = clmHeader;
            var claimsEntities = MockRepository.GenerateStub<IClaimsQuery>();
            claimsEntities.Stub(a => a.GetClaimTransactionHeader(0)).IgnoreArguments().Return(this.claimHeader.InProgressClaimTransactionHeaders.Single());
            container.RegisterInstance<IClaimsQuery>(claimsEntities);

            this.gradesforStructure = new List<Grade> { CreateGrade(GradeCode2, 3), CreateGrade(GradeCode1, 1) };
            this.usersForGradeCode1 = new List<User> { this.currentUser, this.targetUser1 };
            this.usersForGradeCode2 = new List<User>();
            this.eventDestinations = new List<IEventDestination> { CreateEventDestination(this.targetUser1.UserIdentity, "1"), CreateEventDestination(this.targetUser1.UserIdentity, "2") };

            this.metadataEntities = MockRepository.GenerateStub<IMetadataQuery>();
            this.metadataEntities.Stub(a => a.GetUserIdByUserIdentity(this.currentUser.UserIdentity)).Return(this.currentUser.UserID);
            this.metadataEntities.Stub(a => a.GetUserGradeCode(this.currentUser.UserID, GradeStructureCode, StaticValues.GradeType.Claims)).Do(new Func<long, string, StaticValues.GradeType, string>((x, y, z) => this.userGradeCode));
            this.metadataEntities.Stub(a => a.GetGradesForGradeStructure(GradeStructureCode)).Return(this.gradesforStructure);
            this.metadataEntities.Stub(a => a.GetUsersByGradeCode(GradeCode1)).Return(this.usersForGradeCode1);
            this.metadataEntities.Stub(a => a.GetUsersByGradeCode(GradeCode2)).Return(this.usersForGradeCode2);
            container.RegisterInstance(this.metadataEntities);

            var taskService = MockRepository.GenerateStub<ITaskService>();
            taskService.Stub(a => a.GetActiveEventDestinations(null, null, DateTime.Now, DateTime.Now)).IgnoreArguments().Return(this.eventDestinations);
            taskService.Stub(a => a.GetFinishedEventDestinationByDateRange(null, null, DateTime.Now, DateTime.Now)).IgnoreArguments().Return(this.eventDestinations);
            container.RegisterInstance(taskService);


            IAXAClaimsQuery claimsQuery = MockRepository.GenerateStub<IAXAClaimsQuery>();
            claimsQuery.Stub(s => s.IsUserOutOfOffice(Arg<long>.Is.Anything, out Arg<string>.Out(String.Empty).Dummy)).Return(false);
            container.RegisterInstance<IAXAClaimsQuery>(claimsQuery);

            this.businessSupportRole = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("BusinessSupportRole");
        }

        //// No ClaimDetail having ClaimDetailTypeCode = 'TPI' as in Test Initialise
        [TestMethod]
        public void ResolveUser_UserHasNoMatchingGradeNoCDTypeCodeTPI_LowestLevelGradeCodeUsedForInitialLevel()
        {
            this.userGradeCode = null;
            this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });
            this.metadataEntities.AssertWasCalled(a => a.GetGradesForGradeStructure(GradeStructureCode));
        }

        [TestMethod]
        public void ResolveUser_OnlyCurrentUserAvailableForGradeCode_CurrentUserFilterdOut()
        {
            this.usersForGradeCode1.Remove(this.targetUser1);

            this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });
        }

        [TestMethod]
        public void ResolveUser_SingleUserHasAvailableSlots_UserSelectedForAssignment()
        {
            this.targetUser1.CustomNumeric01 = 5;

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.targetUser1.UserIdentity, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.User.ToString(), destination.GetAttribute("DestinationType"));
        }

        [TestMethod]
        public void ResolveUser_UserHasNoSlotLimit_UserSelectedForAssignment()
        {
            this.targetUser1.CustomNumeric01 = null;

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.targetUser1.UserIdentity, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.User.ToString(), destination.GetAttribute("DestinationType"));
        }

        // Out Of Office
        [TestMethod]
        public void ResolveUser_UserHasNoSlotsAvailableOutOfOffice_UserNotSelected()
        {
            this.targetUser1.CustomNumeric01 = 1;
            this.targetUser1.CustomDate01 = DateTime.Now;

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.businessSupportRole, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.Role.ToString(), destination.GetAttribute("DestinationType"));
        }

        [TestMethod]
        public void ResolveUser_RandomUserCanAuthorisePayment_UserGivenTask()
        {
            this.productClaimDefinition.IsManualAuthorisationAlwaysAllowedIfNoChecksAreActive = true;

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.targetUser1.UserIdentity, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.User.ToString(), destination.GetAttribute("DestinationType"));
        }

        [TestMethod]
        public void ResolveUser_RandomUserCanNotAuthorisePayment_UserNotAssignedRole()
        {
            this.productClaimDefinition.IsManualAuthorisationAlwaysAllowedIfNoChecksAreActive = false;

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.businessSupportRole, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.Role.ToString(), destination.GetAttribute("DestinationType"));
        }

        [TestMethod]
        public void ResolveUser_NoAvailableUserForRole_NextHighestGradeSelected()
        {
            this.usersForGradeCode1.Clear();
            this.usersForGradeCode2.Add(this.targetUser2);

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.targetUser2.UserIdentity, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.User.ToString(), destination.GetAttribute("DestinationType"));
        }

        // Not Out Of Office
        [TestMethod]
        public void ResolveUser_UserHasNoSlotsAvailableNotOutOfOffice_UserSelected()
        {
            this.targetUser1.CustomNumeric01 = null;
            this.targetUser1.CustomDate01 = DateTime.Now.AddDays(10);

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.targetUser1.UserIdentity, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.User.ToString(), destination.GetAttribute("DestinationType"));
        }

        // With Claim Detail with Claim Detail Type Code = 'TPI'
        [TestMethod]
        public void ResolveUser_UserHasNoMatchingGradeCDTypeCodeTPI_LowestLevelGradeCodeUsedForInitialLevel()
        {
            this.userGradeCode = null;
            this.claimHeader.ClaimDetails.FirstOrDefault().ClaimDetailTypeCode = ClaimConstants.CLAIMDETAILTYPECODE_TPI;
            this.metadataEntities.Stub(a => a.GetUserGradeCode(this.currentUser.UserID, GradeStructureCode1, StaticValues.GradeType.Claims)).Do(new Func<long, string, StaticValues.GradeType, string>((x, y, z) => this.userGradeCode));
            this.metadataEntities.Stub(a => a.GetGradesForGradeStructure(GradeStructureCode1)).Return(this.gradesforStructure);

            this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            this.metadataEntities.AssertWasCalled(a => a.GetGradesForGradeStructure(GradeStructureCode1));
        }

        [TestMethod]
        public void ResolveUser_UserHasNoMatchingGradeNoClaimHeaderAnalysisCode01_TaskBussinessSupportRole()
        {
            this.targetUser1.CustomNumeric01 = 1;
            this.claimHeader.ClaimHeaderAnalysisCode01 = string.Empty;

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.businessSupportRole, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.Role.ToString(), destination.GetAttribute("DestinationType"));
        }

        [TestMethod]
        public void ResolveUser_UserHasNoMatchingGradeClaimHeaderAnalysisCode01valTEST_TaskBussinessSupportRole()
        {
            this.targetUser1.CustomNumeric01 = 1;
            this.claimHeader.ClaimHeaderAnalysisCode01 = "TEST";

            var destination = this._userResolver.GetData(new Dictionary<string, object> { { "claimTransactionHeaderId", 1 }, { "creatorUserIdentity", this.currentUser.UserIdentity } });

            Assert.AreEqual(this.businessSupportRole, destination.GetAttribute("DestinationName"));
            Assert.AreEqual(StaticValues.DestinationType.Role.ToString(), destination.GetAttribute("DestinationType"));
        }

        private static Grade CreateGrade(string code, short level)
        {
            return new Grade { Code = code, GradeLevel = level };
        }

        private static User CreateUser(string userIdentitiy, long userID, int maxCount)
        {
            return new User { UserIdentity = userIdentitiy, UserID = userID, CustomNumeric01 = maxCount };
        }

        private static IEventDestination CreateEventDestination(string destinationName, string serialNumber)
        {
            var eventDestination = MockRepository.GenerateStub<IEventDestination>();
            eventDestination.SerialNumber = serialNumber;
            eventDestination.DestinationName = destinationName;
            eventDestination.DestinationType = (short)StaticValues.DestinationType.User;

            return eventDestination;
        }

        private void RegisterAuthorityChecks(UnityContainer container)
        {
            var transactionCalculation = MockRepository.GenerateStub<IFinancialCalculation<ClaimTransactionHeaderArgument>>();
            transactionCalculation.Stub(a => a.Evaluate(null)).IgnoreArguments().Return(new ClaimFinancialSummary());

            var claimDetailCalculation = MockRepository.GenerateStub<IFinancialCalculation<ClaimDetailArgument>>();
            claimDetailCalculation.Stub(a => a.Evaluate(null)).IgnoreArguments().Return(new ClaimFinancialSummary());

            var claimCalculation = MockRepository.GenerateStub<IFinancialCalculation<ClaimHeaderArgument>>();
            claimCalculation.Stub(a => a.Evaluate(null)).IgnoreArguments().Return(new ClaimFinancialSummary());

            var userCalculation = MockRepository.GenerateStub<IFinancialCalculation<ClaimHeaderAndUserArgument>>();
            userCalculation.Stub(a => a.Evaluate(null)).IgnoreArguments().Return(new ClaimFinancialSummary());

            container.RegisterInstance<IFinancialCalculation<ClaimTransactionHeaderArgument>>(CheckType.TransactionPaymentAmount.ToString(), transactionCalculation);
            container.RegisterInstance<IFinancialCalculation<ClaimDetailArgument>>(CheckType.TotalClaimDetailPaymentAmount.ToString(), claimDetailCalculation);
            container.RegisterInstance<IFinancialCalculation<ClaimHeaderArgument>>(CheckType.TotalClaimPaymentAmount.ToString(), claimCalculation);
            container.RegisterInstance<IFinancialCalculation<ClaimHeaderAndUserArgument>>(CheckType.TotalClaimPaymentAmountByUser.ToString(), userCalculation);
            container.RegisterInstance<IFinancialCalculation<ClaimDetailArgument>>(CheckType.TotalClaimDetailIncurredAmount.ToString(), claimDetailCalculation);
            container.RegisterInstance<IFinancialCalculation<ClaimHeaderArgument>>(CheckType.TotalClaimIncurredAmount.ToString(), claimCalculation);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
    }
}
