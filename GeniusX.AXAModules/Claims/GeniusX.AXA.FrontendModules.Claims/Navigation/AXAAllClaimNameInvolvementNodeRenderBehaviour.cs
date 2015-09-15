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
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common.ClaimService;
using Xiap.Metadata.Data.Enums;
using System.Collections.Generic;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAAllClaimNameInvolvementNodeRenderBehaviour : INodeRenderBehaviour
    {
        /// <summary>
        /// To extract driver and claimant from nameinvolvement list(other related parties) 
        /// </summary>
        /// <param name="controller">Claim Controller</param>
        /// <param name="context">Context for binding</param>
        public void SetRenderContext(IViewController controller, object context)
        {
            ClaimModel claimModel = (ClaimModel)controller.Model;
            claimModel.NameInvolvementModel.CanAddProductClaimNI = true;
            claimModel.NameInvolvementModel.ProductClaimNameInvolvementTypeForFilter = null;
            claimModel.NameInvolvementModel.ProductClaimNameNameUsageTypeCodeForFilter = string.Empty;
            List<ClaimNameInvolvementDto> claimnameinvolvementdtos = new List<ClaimNameInvolvementDto>();
            foreach (ClaimNameInvolvementDto claimnameinvolvementdto in claimModel.NameInvolvementModel.ClaimNameInvolvementDtos)
            {
                if (claimnameinvolvementdto.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant && claimnameinvolvementdto.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.Driver)
                {
                    if (claimnameinvolvementdto.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
                    {
                        claimnameinvolvementdtos.Add(claimnameinvolvementdto);
                    }
                }
            }

            var claimControllerBase = (IClaimControllerBase)controller;
            claimControllerBase.FillClaimNameInvolvementsList(claimnameinvolvementdtos);
            claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData = null;
            claimModel.NameInvolvementModel.SelectedClaimNameInvolvementRow = null;
        }
    }
}
