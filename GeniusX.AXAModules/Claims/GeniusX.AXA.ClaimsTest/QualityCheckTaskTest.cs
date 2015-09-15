using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xiap.Framework;
using Rhino.Mocks;
using Xiap.Framework.Data.Tasks;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Xiap.Claims.BusinessComponent;
using Xiap.Testing.Utils;
using Xiap.Metadata.Data.Enums;
using Xiap.Metadata.BusinessComponent;
using Xiap.Framework.BusinessTransaction;

namespace GeniusX.AXA.ClaimsTest
{
	[TestClass]
	public class QualityCheckTaskTest
	{
        private const string QualityControlEventTypeCode = "QualityControlEventTypeCode";
        private const string QualityControlProcess = "QualityControlProcess";
        private const string ClaimUserRoleCode = "ClaimUserRoleCode";
		private QualityCheckTask qualityCheckTask;
		private List<ITaskProcess> processes;
		private MockBusinessTransaction claimEventTransaction;
		private ClaimHeader claimHeader;
		private ClaimEvent targetClaimEvent;
		private User targetUser;
		private ITaskService taskService;
		private IClaimsQuery claimEntities;

		[TestCleanup]
		public void Cleanup()
		{
			ObjectFactory.Instance = null;
		}

		[TestInitialize]
		public void Initialize()
		{
			string qualityControlEventTypeCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(QualityControlEventTypeCode);
			string processName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(QualityControlProcess);
			string claimUserRoleCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(ClaimUserRoleCode);

			var container = new UnityContainer();
			ObjectFactory.Instance = new ObjectFactory(container);
			this.qualityCheckTask = new QualityCheckTask();
			this.processes = new List<ITaskProcess>();

			List<string> processNames = new List<string>();

			object[] parameters = new object[] { XiapConstants.XIAP_DATASOURCE };
			this.taskService = MockRepository.GenerateStub<ITaskService>();
			container.RegisterInstance(this.taskService);
			this.taskService.Stub(a => a.GetFinishedTasksForUserByDateRange(null, null, DateTime.Now, DateTime.Now)).IgnoreArguments().Return(this.processes);

			this.claimEntities = MockRepository.GenerateStub<IClaimsQuery>();
            container.RegisterInstance<IClaimsQuery>(this.claimEntities);
	
			this.claimHeader = new BusinessComponentBuilder<ClaimHeader>()
					.SetProperty(a => a.ClaimReference = "CH Reference")
					.Add(new BusinessComponentBuilder<ClaimDetail>()
						.SetProperty(a => a.ClaimDetailReference = "CD1 Reference"))
                    .Add(new BusinessComponentBuilder<ClaimInvolvement>()
						.SetProperty(a => a.ClaimInvolvementType = (short)StaticValues.LinkableComponentType.NameInvolvement)
                        .Add(new BusinessComponentBuilder<ClaimNameInvolvement>()
							.SetProperty(a => a.NameInvolvementType = (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MainClaimHandler)
							.SetProperty(a => a.NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
							.SetProperty(a => a.NameID = 2)))
					.Build();
			
			this.targetUser = new User { UserID = 999, UserIdentity = "TestUser" };
			var metadataEntities = MockRepository.GenerateStub<IMetadataQuery>();
			container.RegisterInstance(metadataEntities);
			metadataEntities.Stub(a => a.GetUserByNameId(2)).Return(this.targetUser);
			metadataEntities.Stub(a => a.GetUsersByRole("Developer")).IgnoreArguments().Return(new List<User>() { this.targetUser });

			MockRepository repository = new MockRepository();
			container.RegisterInstance(MockRepository.GenerateStub<ICopyValidation>());
			this.claimEventTransaction = new MockBusinessTransaction(new TransactionContext(String.Empty, String.Empty,String.Empty));
			this.claimEventTransaction.Context.SetUsage(BusinessProcessUsage.Updateable);
			this.targetClaimEvent = new ClaimEvent();
			this.claimEventTransaction.Component = this.targetClaimEvent;
			container.RegisterInstance<IBusinessTransaction>("Claims.CreateEvent", this.claimEventTransaction);
		}

		[TestMethod]
        [Ignore]
		public void Invoke_QualityReviewEvent_Added_TaskInitialUserCustomRef1()
		{
			this.processes.Add(this.CreateProcess("CHRef", "ReviewTaskFlow_Auto"));
			this.processes.Add(this.CreateProcess("CHRef", "ReviewTaskFlow_Manual"));
			this.taskService.Stub(a => a.GetProcessCountByProcessNameAndDateRange(null, null, DateTime.Now, DateTime.Now)).IgnoreArguments().Return(0);
			this.targetUser.CustomReference01 = "999";
			this.claimEntities.Stub(a => a.GetMainClaimHandlerFromClaim("TestClaimReference")).IgnoreArguments().Return(this.claimHeader.ClaimInvolvements[0].ClaimNameInvolvements[0]);


			this.qualityCheckTask.Invoke(null);
			Assert.AreEqual(this.targetClaimEvent.TaskInitialUserID.ToString(), this.targetUser.CustomReference01);
			Assert.IsNotNull(this.claimEventTransaction.DoStartProperties);
			Assert.AreEqual("CHRef", this.claimEventTransaction.DoStartProperties[0]);
		}

		[TestMethod]
        [Ignore]
		public void Invoke_QualityReviewEvent_Added_TaskInitialUserManager()
		{
			this.processes.Add(this.CreateProcess("CHRef", "ReviewTaskFlow_Auto"));
			this.processes.Add(this.CreateProcess("CHRef", "ReviewTaskFlow_Manual"));
			this.taskService.Stub(a => a.GetProcessCountByProcessNameAndDateRange(null, null, DateTime.Now, DateTime.Now)).IgnoreArguments().Return(0);
			this.targetUser.ManagerID = 9999;
			this.claimEntities.Stub(a => a.GetMainClaimHandlerFromClaim("TestClaimReference")).IgnoreArguments().Return(this.claimHeader.ClaimInvolvements[0].ClaimNameInvolvements[0]);


			this.qualityCheckTask.Invoke(null);
			Assert.AreEqual(this.targetClaimEvent.TaskInitialUserID, this.targetUser.ManagerID);
			Assert.IsNotNull(this.claimEventTransaction.DoStartProperties);
			Assert.AreEqual("CHRef", this.claimEventTransaction.DoStartProperties[0]);
		}

		[TestMethod]
		public void Invoke_QualityReviewEventExists_NotQualityEventAdded()
		{
			this.processes.Add(this.CreateProcess("CHRef", "ReviewTaskFlow_Auto"));
			this.processes.Add(this.CreateProcess("CHRef", "ReviewTaskFlow_Manual"));
			this.taskService.Stub(a => a.GetProcessCountByProcessNameAndDateRange(null, null, DateTime.Now, DateTime.Now)).IgnoreArguments().Return(1);
			this.targetUser.CustomReference01 = "1";
			this.claimEntities.Stub(a => a.GetMainClaimHandlerFromClaim("TestClaimReference")).IgnoreArguments().Return(this.claimHeader.ClaimInvolvements[0].ClaimNameInvolvements[0]);

			this.qualityCheckTask.Invoke(null);
			Assert.IsNull(this.claimEventTransaction.DoStartProperties);
		}

		[TestMethod]
		public void Invoke_NoReviewProcessExists_NotQualityEventAdded()
		{
			this.qualityCheckTask.Invoke(null);
			Assert.IsNull(this.claimEventTransaction.DoStartProperties);
		}

		[TestMethod]
		public void Invoke_UserNotClaimHandler_NotQualityEventAdded()
		{
			this.claimEntities.Stub(a => a.GetMainClaimHandlerFromClaim("TestClaimReference")).IgnoreArguments().Return(null);
			this.processes.Add(this.CreateProcess("CHRef", "ReviewTaskFlow_Auto"));
			this.processes.Add(this.CreateProcess("CHRef", "ReviewTaskFlow_Manual"));

			this.qualityCheckTask.Invoke(null);
			Assert.IsNull(this.claimEventTransaction.DoStartProperties);
		}

		private ITaskProcess CreateProcess(string folio, string processName)
		{
			var process = MockRepository.GenerateStub<ITaskProcess>();
			process.Folio = folio;
			process.ProcessName = processName;

			return process;
		}
	}
}
