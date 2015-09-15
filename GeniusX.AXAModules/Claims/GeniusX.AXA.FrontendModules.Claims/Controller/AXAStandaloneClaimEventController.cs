using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Claims.Model;
using XIAP.Frontend.CommonInfrastructure.Model.Events;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.FrontendModules.Claims.Data;
using XIAP.FrontendModules.Claims.ViewResolver;
using GeniusX.AXA.FrontendModules.Claims.Model;
using System.ComponentModel;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.Frontend.Infrastructure.DataMapping;
using Xiap.ClientServices.Facade.Common;
using XIAP.Frontend.CoreControls;
using GeniusX.AXA.FrontendModules.Claims.Resources;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAStandaloneClaimEventController : StandaloneClaimEventController
    {
        private AXAEventModel _axaEventModel;
        public AXAStandaloneClaimEventController(IClaimClientService claimService, IMetadataClientService metadataService, StandaloneClaimEventModel model, AXAEventModel eventModel, IUnityContainer container, AppModel appModel, ClaimsRetrievalManager retrievalManager, ClaimsPayloadManager payloadManager, IStandaloneClaimEventViewResolver viewResolver)
            : base(claimService, metadataService, model, eventModel, appModel, retrievalManager, payloadManager, viewResolver, container)
        {
            this.Model  = model;
            this._axaEventModel = eventModel;
        }

        /// <summary>
        /// Validate the event and set default task initial user Id, if Task Initial User Id is null.
        /// </summary>
        /// <param name="obj">object type parameter</param>
        /// <param name="args">event arguements</param>
        protected override void ValidateEvent(object obj, EventArgs args)
        {
            var claimEventData = this.Model.EventDto.EventData as ClaimEventData;

            if (claimEventData != null)
            {
                if (claimEventData.EventTypeCode == AXAClaimConstants.EVENT_PHONE_TYPECODE || claimEventData.EventTypeCode == AXAClaimConstants.EVENT_REVIEW_TYPECODE)
                {
                    if (this._axaEventModel.DefaultTaskInitialUserID == null)
                    {
                        this._axaEventModel.DefaultTaskInitialUserID = this.Model.EventDto.EventData.TaskInitialUserID;
                    }

                    if (this.Model.EventModel.UserIdentity == string.Empty)
                    {
                        this.Model.EventDto.EventData.TaskInitialUserID = this._axaEventModel.DefaultTaskInitialUserID;
                        var theView = this.Navigator.NavGraph.NavNodes.Single().NodeName;
                        this.Navigator.Open(theView);
                    }
                }
            }

            base.ValidateEvent(obj, args);
        }
    }
}
