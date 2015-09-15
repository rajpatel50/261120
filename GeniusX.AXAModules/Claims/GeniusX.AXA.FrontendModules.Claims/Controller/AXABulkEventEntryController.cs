using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using Xiap.Framework.Data;
using XIAP.Frontend.Infrastructure;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.Frontend.Infrastructure.DataMapping;
using XIAP.FrontendModules.Claims.Data;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXABulkEventEntryController : BulkEventEntryController
    {
        private readonly IClaimClientService claimService;

        public AXABulkEventEntryController(IUnityContainer container, IClaimClientService claimService, AXABulkEventEntryModel model, AppModel appModel, ClaimsPayloadManager payloadManager, ClaimsRetrievalManager claimsRetrievalManager)
            : base(claimService, model, appModel, payloadManager, claimsRetrievalManager, container)
        {
            this.claimService = claimService;
        }

        protected override void ClaimHeaderReferenceEventRequest(string reference)
        {
            if (!string.IsNullOrWhiteSpace(reference))
            {
                this.claimService.GetTaskInitialUserAndEventTypesForEventRequest(reference, true, this.LoadEventType, this.HandleError);
            }
            else
            {
                this.lastAlteredRow.IsReferenceValid = true;
                this.ClearMessage(this.lastAlteredRow.RowID);
                Model.RaiseCanExecuteChangeCommand();
            }
        }

        protected override void ProcessResponseOnListEvent(ClaimEventEntryResponse claimResponse)
        {
            if (claimResponse.PriorityCodes != null && claimResponse.PriorityCodes.Count > 0)
            {
                this.Model.PriorityCodes = claimResponse.PriorityCodes.ToObservableCollection();
            }

            this.Model.ClaimEventRequestsCopy = claimResponse.ClaimEventRequests; //// Create a copy for verification

            this.Model.BulkEvents.Clear();
            foreach (ClaimEventRequestData eventRequest in claimResponse.ClaimEventRequests)
            {
                AXABulkEventEntryRow row = new AXABulkEventEntryRow()
                {
                    EventRequestID = eventRequest.EventRequestID,
                    ClaimHeaderReference = eventRequest.ClaimReference,
                    HeaderStatusCodeDescription = eventRequest.ClaimHeaderStatusDescription,
                    IsClaimHeaderReferenceEnabled = false,
                    TaskInitialUserID = eventRequest.TaskInitialUserID,
                    TaskInitialUserDisplayValue = eventRequest.TaskInitialUserDisplayName,
                    EventTypeCollection = eventRequest.EventTypes,
                    EventType = eventRequest.EventType,
                    PriorityAllowedValues = this.Model.PriorityCodes,
                    Priority = eventRequest.Priority,
                    EventRequestDetails = eventRequest.EventRequestDetails,
                    IsProcessed = eventRequest.IsProcessed.Value,
                    CreatedDate = eventRequest.CreatedDate,
                    CreatedByUserID = eventRequest.CreatedByUserId,
                    CreatedByUserDisplayValue = eventRequest.CreatedByUserDisplayName,
                    RevisionNumber = eventRequest.RevisionNumber,
                    BusinessDataState = eventRequest.BusinessDataState,
                    TaskInitialPriority = eventRequest.TaskInitialPriority, 
                    SearchPopupAction = this.Model.SearchPopupAction,
                    SearchControllerName = this.Model.SearchControllerName,
                    ParentModel = this.Model
                };

                this.Model.BulkEvents.Add(row);
            }

            this.AddNewBulkEventRow();
            this.Model.SelectedBulkEventRow = this.Model.BulkEvents[this.Model.BulkEvents.Count - 1];
            this.Model.IsBusy = false;
            this.Model.RaiseCanExecuteChangeCommand();
        }

        protected override void AddNewBulkEventRow()
        {
            AXABulkEventEntryRow bulkEventRow = new AXABulkEventEntryRow()
            {
                PriorityAllowedValues = this.Model.PriorityCodes,
                SearchPopupAction = this.Model.SearchPopupAction,
                SearchControllerName = this.Model.SearchControllerName,
                ParentModel = this.Model
            };

            this.Model.BulkEvents.Add(bulkEventRow);
        }

        private void LoadEventType(ClaimEventEntryResponse claimEventResponse)
        {
            if (this.lastAlteredRow != null)
            {
                this.lastAlteredRow.IsReferenceValid = claimEventResponse.Status == BusinessTransactionStatus.Valid;
                this.ValidateResponse(claimEventResponse, null, false, claimEventResponse.Status, this.lastAlteredRow.RowID);

                if (this.lastAlteredRow.IsReferenceValid)
                {
                    if (claimEventResponse.EventTypes != null)
                    {
                        this.lastAlteredRow.EventTypeCollection = claimEventResponse.EventTypes;

                        if (this.lastAlteredRow.BusinessDataState == BusinessDataState.Added &&
                            string.IsNullOrEmpty(this.lastAlteredRow.EventType))
                        {
                            if (this.lastAlteredRow.EventTypeCollection.Any(a => a.Code == AXAClaimConstants.DefaultBulkEventType))
                            {
                                this.lastAlteredRow.EventType = AXAClaimConstants.DefaultBulkEventType;
                            }
                        }
                    }

                    this.lastAlteredRow.TaskInitialUserID = claimEventResponse.TaskInitialUserID;
                    this.lastAlteredRow.ProductVersionID = claimEventResponse.ProductVersionID;
                    this.lastAlteredRow.ClaimHeaderID = claimEventResponse.ClaimHeaderID;
                }
                else
                {
                    this.lastAlteredRow.ClaimHeaderReference = Model.HeaderReference;
                    if (Model.HeaderReference == string.Empty)
                    {
                        Model.HeaderReference = null;
                    }
                    else
                    {
                        Model.HeaderReference = string.Empty;
                    }
                }

                Model.RaiseCanExecuteChangeCommand();
            }
        }

        protected override void ProcessFinishComplete(ClaimResponse response)
        {
            AXABulkEventEntryModel model = (AXABulkEventEntryModel)this.Model;
            model.HasCascadingCalls = true;
            model.SelectedBulkEventRow.IsProcessed = true;
            model.OnClaimSave(null);
            base.ProcessFinishComplete(response);
        }
    }
}
