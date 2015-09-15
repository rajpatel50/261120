using System;
using System.Collections.Generic;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using Xiap.Framework.Validation;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Data;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common.Controller;
using XIAP.FrontendModules.Common.TaskService;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAManualAuthorisationController : ManualAuthorisationController
    {
        private IClaimClientService claimClientService;
        private TaskInitiatedContollerArgs args;
        private TaskArgumentsHolder holder;
        private ClaimPaymentRejectReasonModel payRejectmodel;
        private long claimTransactionHeaderID;

        public AXAManualAuthorisationController(IUnityContainer container, IClaimClientService claimClientService, ManualAuthorisationModel manualAuthorisationModel, ClaimsPayloadManager claimPayloadManager, INavigator navigator, ClaimPaymentRejectReasonModel paymentRejectReasonModel, AppModel appModel)
            : base(claimClientService, manualAuthorisationModel, claimPayloadManager, navigator, container, appModel)
        {
            this.claimClientService = claimClientService;
            this.payRejectmodel = paymentRejectReasonModel;
            this.Navigator = navigator;
            this.payRejectmodel.OnOk += this.OkClicked;
            this.payRejectmodel.OnCancel += this.CancelClicked; 
        }

        public ClaimPaymentRejectReasonModel AuthorisationModel
        {
            get
            {
                return this.payRejectmodel;
            }
        }

        public override void OnStart(TaskArgumentsHolder holder)
        {
            ArgumentCheck.ArgumentNullCheck(holder, "holder");
            ArgumentCheck.ArgumentNullCheck(holder.ControllerArguments, "holder.ControllerArguments");
            this.holder = holder;
            if (holder.ControllerArguments != null)
            {
                this.args = (TaskInitiatedContollerArgs)holder.ControllerArguments;
                this.Model.IsBusy = true;
                TaskDetail taskDetails = this.args.TaskDetail;
                Dictionary<string, object> activityData = taskDetails.Data;
                if (!activityData.ContainsKey("ClaimTransactionHeaderID"))
                {
                    throw new ArgumentException("activity data must contain ClaimTransactionHeaderID");
                }

                this.claimTransactionHeaderID = Convert.ToInt64(activityData["ClaimTransactionHeaderID"]);
            }
        }

        private void OkClicked(object sender, EventArgs args)
        {
            this.args.CompletionStatus = CompletionStatus.Complete;
            this.args.TaskDetail.Data = new System.Collections.Generic.Dictionary<string, object>();
            this.args.TaskDetail.Data["RejectionReason"] = this.payRejectmodel.RejectionReason;
            
            if (this.TransactionAction == TransactionAction.Reject)
            {
                this.RejectClaimTransaction(this.claimTransactionHeaderID, this.holder, this.args);
            }
        }

        private void CancelClicked(object sender, EventArgs args)
        {
            this.args.CompletionStatus = CompletionStatus.Cancelled;
            this.Navigator.Finish(this.args);
        }
    }
}
