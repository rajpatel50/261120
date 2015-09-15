using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using Xiap.Framework.Metadata;
using XIAP.Frontend.CommonInfrastructure.Model.Events;
using XIAP.Frontend.Infrastructure;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.Events;
using System.Collections.Generic;
using System.Linq;
using XIAP.FrontendModules.Common.ClaimService;
using GeniusX.AXA.FrontendModules.Claims.Model;
using XIAP.Frontend.Infrastructure.DataMapping;
using Xiap.ClientServices.Facade.Common;
using XIAP.FrontendModules.Infrastructure.NavTree;
using Xiap.Framework.Data;
using XIAP.FrontendModules.Claims.Resources;
namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAClaimEventController : ClaimEventController<ClaimModel>
    {
        private ClaimControllerBase<ClaimModel> _claimController;
        public AXAClaimEventController(ClaimControllerBase<ClaimModel> claimController, ClaimModel claimModel, EventModel eventModel, IUnityContainer container, IClaimClientService claimClientService, IMetadataClientService metadataService, AppModel appModel, IEventAggregator eventAggregator)
            : base(claimController, claimModel, eventModel, claimClientService, metadataService, appModel, eventAggregator,container)
        {
            this._claimController = claimController;
            this.Model = eventModel as AXAEventModel;
            ((AXAEventModel)this.Model).ClaimEventPropertyChanged = new EventHandler<PropertyChangedEventArgs>(this.Event_ClaimEventPropertyChanged);
        }

        /// <summary>
        /// method calls when Event property changed.
        /// </summary>
        /// <param name="sender">object type</param>
        /// <param name="args">Event arguments of property changed</param>
        private void Event_ClaimEventPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            this.Events_PropertyChanged(sender, args);
        }

        /// <summary>
        /// method calls when edit and start edit event graph.
        /// </summary>
        protected override void StartEditEventGraph()
        {
            TreeNodeData<ActionKey> node = this.LoadEventTreeNode();
            if (node != null)
            {
                node.Sync(NavigationType.Events.ToString());
            }

            if (this.Model.SelectedEventDto != null)
            {
                ActionKey key = this._claimController.Model.ClaimTreeModel.GetActionKey((this.Model.SelectedEventDto as DtoBase).Data.DataId);
                if (key != null && key.NodeInfo != null)
                {
                    key.NodeInfo.Sync(this.Model.SelectedEventDto);
                }
            }

            ClaimEventData claimEventData = (ClaimEventData)this._claimController.Model.EventModel.SelectedEventDto.EventData;

            if (claimEventData.EventTypeCode == AXAClaimConstants.EVENT_POST_TYPECODE && claimEventData.CustomCode02 == AXAClaimConstants.EVENT_PRIORITY_REC)
            {
                this._claimController.Model.EventModel.UserRow = null;
                this._claimController.Model.EventModel.UserIdentity = string.Empty;
                claimEventData.TaskInitialUserID = null;
                this._claimController.Model.EventModel.EventsFields.TaskInitialUserId.Readonly = true;
                this._claimController.Model.EventModel.EventsFields.SetField("TaskInitialUserId", this._claimController.Model.EventModel.EventsFields.TaskInitialUserId);
            }

            this._claimController.Model.RefreshModelFields(typeof(EventFields));
            this.Model.Details = this.PopulateDetails();
            this.Model.SelectedDetail = this.GetSelectedDetail(this.Model.SelectedEventDto.EventData);
            this._claimController.Model.SelectedDto = this.Model.SelectedEventDto as DtoBase;
            // passing selectedeventdto to resolve the error during save event
            this._claimController.SetSelectedClaimTreeNode(this._claimController.Model.TreeModel, (DtoBase)this.Model.SelectedEventDto);
        }

        /// <summary>
        /// method calls when property of event is changed.
        /// </summary>
        /// <param name="sender">object type</param>
        /// <param name="e">event arguements</param>
        protected override void Events_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.Events_PropertyChanged(sender, e);
            ClaimEventData claimEventData = sender as ClaimEventData;
            if (e.PropertyName == "CustomCode02")
            {
                if (claimEventData.CustomCode02 == AXAClaimConstants.EVENT_PRIORITY_REC && claimEventData.EventTypeCode == AXAClaimConstants.EVENT_POST_TYPECODE)
                {
                    if (this.Model.UserIdentity == string.Empty)
                    {
                        this.Model.UserIdentity = null;
                    }

                    this.Model.UserIdentity = string.Empty;
                    claimEventData.TaskInitialUserID = null;
                    this.Model.EventsFields.TaskInitialUserId.Readonly = true;
                    this.Model.EventsFields.SetField("TaskInitialUserId", this.Model.EventsFields.TaskInitialUserId);
                }
                else if (claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_CLAIM_CREATED_TYPECODE) || claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_INACTIVITY_REVIEW_TYPECODE) || claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_PAYMENT_CANCELLATION_TYPECODE)
                    || claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_PHONE_TYPECODE) || claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_POST_TYPECODE) || claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_REPORT_REVIEW_TYPECODE)
                    || claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_REVIEW_TYPECODE) || claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_RECOVERY_INACTIVITY_REVIEW_TYPECODE) || claimEventData.EventTypeCode.Equals(AXAClaimConstants.EVENT_REOPEN_CLAIM_TYPECODE))
                {
                    if (string.IsNullOrEmpty(this.Model.UserIdentity))
                    {
                        this.Model.UserRow = null;
                        this.Model.UserIdentity = null;
                        claimEventData.TaskInitialUserID = null;

                        if (((AXAEventModel)this.Model).DefaultTaskInitialUserID != null)
                        {
                            claimEventData.TaskInitialUserID = ((AXAEventModel)this.Model).DefaultTaskInitialUserID;
                        }
                        else
                        {
                            this._payloadManager.VirtualProcess = VirtualProcessRequestBuilder.BuildComponentVirtualProcessRequest(this._payloadManager, (sender as ClaimEventData).DataId, "AXADefaultClaimEventValuesPreComplete");
                            this._payloadManager.SynchroniseData(this.Model.EventContainerDto,
                                this.Model.TransactionId,
                                (Response r) =>
                                {
                                    ((AXAEventModel)this.Model).DefaultTaskInitialUserID = claimEventData.TaskInitialUserID;
                                    this.Model.IsBusy = false;
                                });
                        }

                        this.StartEditEventGraph();

                        this.Model.EventsFields.TaskInitialUserId.Readonly = false;
                        this.Model.EventsFields.SetField("TaskInitialUserId", this.Model.EventsFields.TaskInitialUserId);
                    }
                }
            }
        }
    }
}
