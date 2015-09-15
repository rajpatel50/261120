using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Xiap.Framework.Metadata;
using XIAP.Frontend.CommonInfrastructure.Model.Events;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common.ClaimService;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimsEventNodeRenderBehaviour : INodeRenderBehaviour
    {
        public void SetRenderContext(XIAP.Frontend.Infrastructure.IViewController controller, object context)
        {
            var claimEventDto = (ClaimEventDto)context;
            var claimModel = (ClaimModel)controller.Model;
            claimModel.EventModel.SelectedEventDto = claimEventDto;
            claimModel.SelectedDto = claimEventDto;
            claimModel.EventModel.UserRow = null;
            ClaimEventData claimEventData = (ClaimEventData)claimModel.EventModel.SelectedEventDto.EventData;
            claimModel.EventModel.UserIdentity = string.Empty;

            if (claimEventData.EventTypeCode == AXAClaimConstants.EVENT_POST_TYPECODE && claimEventData.CustomCode02 == AXAClaimConstants.EVENT_PRIORITY_REC)
            {
                claimEventData.TaskInitialUserID = null;
                claimModel.EventModel.EventsFields.TaskInitialUserId.Readonly = true;
                claimModel.EventModel.EventsFields.SetField("TaskInitialUserId", claimModel.EventModel.EventsFields.TaskInitialUserId);
            }

            claimModel.EventModel.Details = this.PopulateDetails(claimModel);
            claimModel.RefreshModelFields(typeof(EventFields));
        }

        private ObservableCollection<CodeRow> PopulateDetails(ClaimModel claimModel)
        {
            ObservableCollection<CodeRow> details = new ObservableCollection<CodeRow>();
            details.Add(new CodeRow() { Code = string.Empty, Description = " " });
            foreach (ClaimDetailDto claimDetailDto in claimModel.HeaderDto.ClaimDetails)
            {
                CodeRow codeRow = new CodeRow();
                codeRow.Code = claimDetailDto.ClaimDetailData.ClaimDetailReference;
                codeRow.Description = claimDetailDto.ClaimDetailData.ClaimDetailReference + (!String.IsNullOrEmpty(claimDetailDto.ClaimDetailData.ClaimDetailTitle) ? (" - " + claimDetailDto.ClaimDetailData.ClaimDetailTitle) : string.Empty);
                details.Add(codeRow);
            }

            claimModel.InvokeCollectionChanged("ClaimEvent", claimModel.HeaderDto.Data.DataId);
            return details;
        }
    }
}
