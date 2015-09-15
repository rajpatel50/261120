using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common.ClaimService;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXADriverNodeRenderBehaviour : INodeRenderBehaviour
    {
        public void SetRenderContext(XIAP.Frontend.Infrastructure.IViewController controller, object context)
        {
            ClaimModel claimModel = (ClaimModel)controller.Model;
            claimModel.SelectedDto = claimModel.NameInvolvementModel.SelectedClaimNameInvolvementDto = (ClaimNameInvolvementDto)context;

            if (claimModel.NameInvolvementModel.SelectedClaimNameInvolvementDto != null)
            {
                claimModel.NameInvolvementModel.AddressList = claimModel.GetAddressList();
            }
            else
            {
                claimModel.NameInvolvementModel.AddressList = new System.Collections.ObjectModel.ObservableCollection<Xiap.Framework.Metadata.CodeRow>();
            }
        }
    }
}
