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
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Navigation.NodeRenderBehaviour;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimDetailNodeRenderBehaviour : INodeRenderBehaviour
    {
        /// <summary>
        /// To show the view on Repair/medical/ACS.
        /// </summary>
        /// <param name="controller">claim controller base</param>
        /// <param name="context">Claim Detail Dto</param>
        public void SetRenderContext(XIAP.Frontend.Infrastructure.IViewController controller, object context)
        {
            var claimModel = (ClaimModel)controller.Model;
            var claimController = (IClaimControllerBase)controller;
            ClaimDetailNodeRenderBehaviour claimdetailnoderenderbehaviour = (ClaimDetailNodeRenderBehaviour)controller.Container.Resolve(typeof(INodeRenderBehaviour), "ClaimDetailNodeRenderBehaviour");
            claimdetailnoderenderbehaviour.SetRenderContext(controller, context);
            claimModel.SelectedDto = null;
        }
    }
}
