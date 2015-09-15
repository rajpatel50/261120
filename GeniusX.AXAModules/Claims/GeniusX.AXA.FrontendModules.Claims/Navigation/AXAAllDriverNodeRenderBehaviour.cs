using System;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common.ClaimService;
using Microsoft.Practices.Unity;
using System.Linq.Expressions;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAAllDriverNodeRenderBehaviour : INodeRenderBehaviour
    {
        private string _nameusagetypecode;
        private StaticValues.NameInvolvementType_ClaimNameInvolvement _nameinvolvementtype;

        [Dependency]
        public string NameUsageTypeCode
        {
            get { return this._nameusagetypecode; }
            set { this._nameusagetypecode = value; }
        }

        [Dependency]
        public StaticValues.NameInvolvementType_ClaimNameInvolvement NameInvolvementType
        {
            get { return this._nameinvolvementtype; }
            set { this._nameinvolvementtype = value; }
        }

        public void SetRenderContext(XIAP.Frontend.Infrastructure.IViewController controller, object context)
        {
            ClaimModel claimModel = (ClaimModel)controller.Model;
            claimModel.NameInvolvementModel.ProductClaimNameInvolvementTypeForFilter = (short)this.NameInvolvementType;
            claimModel.NameInvolvementModel.ProductClaimNameNameUsageTypeCodeForFilter = this.NameUsageTypeCode;
            claimModel.NameInvolvementModel.CanAddProductClaimNI = true;
            var claimControllerBase = (IClaimControllerBase)controller;
            claimControllerBase.FillClaimNameInvolvementsList(claimModel.NameInvolvementModel.ClaimNameInvolvementDtos.ToList());
            // assigned selected nameinvolvement by null to disable edit button when no rows is selecting.
            claimModel.NameInvolvementModel.SelectedClaimNameInvolvementRow = null;
            claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData = null;
        }
    }
}
