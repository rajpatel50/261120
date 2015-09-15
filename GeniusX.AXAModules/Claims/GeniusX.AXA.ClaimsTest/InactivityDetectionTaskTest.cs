using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Data.Tasks;
using Xiap.Framework.Messages;
using Xiap.K2Integration.Task.BusinessComponent;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.Testing.Utils.Mocks;
using Xiap.Framework.Metadata;
using Xiap.Framework.Common;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class InactivityDetectionTaskTest
    {
        private const int MaxClaimDetails = 5;

        private InactivityDetectionTask inactivityTask;
        private List<ITaskProcess> processes;
        private List<IProcessEvent> processEvents;
        private ClaimHeader claimHeader;
        private ClaimEvent targetClaimEvent;
        private User targetUser;
        private MockBusinessTransaction claimEventTransaction;

        private int inactivityThresholdPeriod;
        private string reviewEventyTypeCode;
        private string inactivityDetectionProcess;

        [TestInitialize]
        public void Initialise()
        {
            this.inactivityThresholdPeriod = int.Parse(ConfigurationManager.AppSettings["InactivityThresholdPeriod"]);
            this.reviewEventyTypeCode = ConfigurationManager.AppSettings["ReviewEventyTypeCode"];
            this.inactivityDetectionProcess = ConfigurationManager.AppSettings["InactivityDetectionProcess"];
            
            var container = new UnityContainer();
            ObjectFactory.Instance = new ObjectFactory(container);
            this.inactivityTask = new InactivityDetectionTask();
            this.processes = new List<ITaskProcess>();
            this.processEvents = new List<IProcessEvent>();
            
            var taskService = MockRepository.GenerateStub<ITaskService>();
            container.RegisterInstance(taskService);
            taskService.Stub(a => a.FindActiveProcesses(null)).IgnoreArguments().Return(this.processes);
            taskService.Stub(a => a.FindProcessEvents(null, 0, 0)).IgnoreArguments().Return(this.processEvents);

            this.claimHeader = new BusinessComponentBuilder<ClaimHeader>()
                    .SetProperty(a => a.ClaimReference = "CH Reference")
                    .SetProperty(a => a.CustomCode20 = MaxClaimDetails.ToString())
                    .Add(new BusinessComponentBuilder<ClaimDetail>()
                        .SetProperty(a => a.ClaimDetailReference = "CD1 Reference"))
                    .Add(new BusinessComponentBuilder<ClaimDetail>()
                        .SetProperty(a => a.ClaimDetailReference = "CD2 Reference"))
                    .Add(new BusinessComponentBuilder<ClaimInvolvement>()
                        .SetProperty(a => a.ClaimInvolvementType = (short)StaticValues.LinkableComponentType.NameInvolvement)
                        .Add(new BusinessComponentBuilder<ClaimNameInvolvement>()
                            .SetProperty(a => a.NameInvolvementType = (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MainClaimHandler)
                            .SetProperty(a => a.NameInvolvementMaintenanceStatus = (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
                            .SetProperty(a => a.NameID = 2)))
                    .Build();

            var claimEntities = MockRepository.GenerateStub<IClaimsQuery>();
            this.targetClaimEvent = new ClaimEvent();
            this.targetUser = new User { UserID = 999};

            Func<DateTime, IEnumerable<long>, ILookup<ClaimHeader, ClaimDetail>> returnLookup = (x, y) => this.claimHeader.InternalClaimDetails.ToLookup(a => a.ClaimHeader);
            claimEntities.Stub(a => a.GetInactiveClaims(DateTime.Now, null)).IgnoreArguments().Do((Func<DateTime, IEnumerable<long>, ILookup<ClaimHeader, ClaimDetail>>)returnLookup);
            container.RegisterInstance<IClaimsQuery>(claimEntities);
            MockRepository repository = new MockRepository();
            container.RegisterInstance(MockRepository.GenerateStub<ICopyValidation>());
            this.claimEventTransaction = new MockBusinessTransaction(new TransactionContext(string.Empty, string.Empty, string.Empty));
            this.claimEventTransaction.Context.SetUsage(BusinessProcessUsage.Updateable);
            this.claimEventTransaction.Component = this.targetClaimEvent;
            container.RegisterInstance<IBusinessTransaction>("Claims.CreateEvent", this.claimEventTransaction);

            var metadataEntities = MockRepository.GenerateStub<IMetadataQuery>();
            metadataEntities.Stub(a => a.GetEventTypeVersion(null)).IgnoreArguments().Return(new EventTypeVersion[] { }.AsQueryable());
            metadataEntities.Stub(a => a.GetUserByNameId(2)).Return(this.targetUser);
            container.RegisterInstance(metadataEntities);
            container.RegisterInstance<IMessageService>(new MockMessagingService());

            this.inactivityTask.GetInactiveClaimDetails = this.GetInactiveTestClaims;
        }

        [TestCleanup]
        public void Cleanup()
        {
            ObjectFactory.Instance = null;
        }
        
        [TestMethod]
        public void Invoke_HasClaimHeaderLevelInactivityReview_InactivityNotRecored()
        {
            this.processes.Add(this.CreateProcess("CH Reference"));
            this.claimHeader.CustomCode20 = "1";
            this.processEvents.Add(new ProcessEvent() { ActivityName = "Claim Review" });
            this.inactivityTask.Invoke(null);

            Assert.IsNull(this.claimEventTransaction.DoStartProperties, "shouldn't have called claim event transaction");
        }

        [TestMethod]
        [Ignore]
        public void Invoke_ClaimDetailsDontExceedMaximumActiveClaimDetails_NewClaimDetailEventAdded()
        {
            this.inactivityTask.Invoke(null);

            Assert.AreEqual(this.targetClaimEvent.TaskInitialUserID, this.targetUser.UserID);
            Assert.IsNotNull(this.claimEventTransaction.DoStartProperties);
            Assert.AreEqual("CH Reference", this.claimEventTransaction.DoStartProperties[0]);
            Assert.AreEqual("CD2 Reference", this.claimEventTransaction.DoStartProperties[2]);
        }

        [TestMethod]
        public void Invoke_NoConfiguredMaxiumumActiveClaimDetails_Error()
        {
            this.claimHeader.CustomCode20 = null;
            ScheduledTaskResponse retVal = this.inactivityTask.Invoke(null);
            Assert.IsTrue(retVal.Result == ScheduledTaskResult.Failed);
        }

        [TestMethod]
        [Ignore]
        public void Invoke_ClaimDetailsExceedMaximumActiveClaimDetails_NewClaimEventAdded()
        {
            for (int i = 0; i < MaxClaimDetails; i++)
            {
                this.claimHeader.InternalClaimDetails.Add(new ClaimDetail { ClaimDetailReference = i.ToString() });
            }

            this.inactivityTask.Invoke(null);

            Assert.AreEqual(this.targetClaimEvent.TaskInitialUserID, this.targetUser.UserID);
            Assert.IsNotNull(this.claimEventTransaction.DoStartProperties);
            Assert.AreEqual("CH Reference", this.claimEventTransaction.DoStartProperties[0]);
            Assert.IsNull(this.claimEventTransaction.DoStartProperties[2], "no claim detail reference should be supplied for the claim event transaction");
        }

        private ITaskProcess CreateProcess(string folio)
        {
            var process = MockRepository.GenerateStub<ITaskProcess>();
            process.Folio = folio;
            return process;
        }

        private List<InactivityClaimDetail> GetInactiveTestClaims()
        {
            List<InactivityClaimDetail> InactiveClaims = new List<InactivityClaimDetail>();
            var taskService = ObjectFactory.Resolve<ITaskService>();
            ILookup<ClaimHeader, ClaimDetail> inactiveClaims = ObjectFactory.Resolve<IClaimsQuery>().GetInactiveClaims(DateTime.Now, null);
            foreach (IGrouping<ClaimHeader, ClaimDetail> group in inactiveClaims)
            {
                foreach (var claimDetail in group)
                {
                    var InactiveClaim = new InactivityClaimDetail
                    {
                        ClaimHeaderID = claimDetail.ClaimHeader.ClaimHeaderID,
                        ClaimDetailID = claimDetail.ClaimDetailID,
                        CustomCode20 = claimDetail.ClaimHeader.CustomCode20,
                        ClaimReference = claimDetail.ClaimHeader.ClaimReference,
                        NameID = claimDetail.ClaimHeader.NameInvolvements.Where(o => o.NameInvolvementType == (short)StaticValues.NameInvolvementType.MainClaimHandler).Select(o => o.NameID).First(),
                        ClaimDetailReference = claimDetail.ClaimDetailReference,
                        ClaimTransactionSource = 1,
                    };

                    // Review event at Header level?
                    bool Revieweventexist = this.IsInactivityTaskAlreadyExists(claimDetail.ClaimHeader.ClaimHeaderID, this.inactivityDetectionProcess, "Claim Review", SystemComponentConstants.ClaimHeader, taskService);
                    if (!Revieweventexist)
                    {
                        // Review event at Detail level?
                        Revieweventexist = this.IsInactivityTaskAlreadyExists(claimDetail.ClaimDetailID, this.inactivityDetectionProcess, "Claim Review", SystemComponentConstants.ClaimDetail, taskService);
                    }

                    if (!Revieweventexist)
                    {
                        InactiveClaims.Add(InactiveClaim);
                    }
                }
            }

            return InactiveClaims;
        }

        private bool IsInactivityTaskAlreadyExists(long componentId, string processName, string activityName, long systemComponentId, ITaskService taskService)
        {
            var processEvents = taskService.FindProcessEvents(processName, componentId, systemComponentId);
            if (string.IsNullOrWhiteSpace(activityName))
            {
                return processEvents.Count() > 0;
            }
            else
            {
                return processEvents.Where(processEvent => processEvent.ActivityName == activityName).Any();
            }
        }
    }
}
