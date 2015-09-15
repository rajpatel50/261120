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
using Xiap.Framework.Data;
using XIAP.Frontend.Infrastructure;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Navigation.AvilabilityBehaviours;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.ClaimsService;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimDetailLinksNodeAvailabilityBehaviour:ClaimDetailLinksNodeAvailabilityBehaviour
    {
        public AXAClaimDetailLinksNodeAvailabilityBehaviour(IConverter<DtoBase, long?> productLinkableComponentIDConverter, IConverter<ITransactionController, ClaimModel> transactionControllerConverter) :
            base(productLinkableComponentIDConverter, transactionControllerConverter)
        {
        }

        protected override bool IsAvailableFor(XIAP.FrontendModules.Claims.Model.ClaimModel claimModel, Xiap.Framework.Data.DtoBase parentDto)
        {
            IClaimLitigationData claimlitigationdata = null;
            claimlitigationdata = parentDto.Data as ClaimLitigationData;

            if (base.IsAvailableFor(claimModel, parentDto) && (claimlitigationdata.LitigationType == AXAClaimConstants.LITIGATIONTYPE_LIT || claimlitigationdata.LitigationType == AXAClaimConstants.LITIGATIONTYPE_OTH))
            {
                return true;
            }

            return false;
        }
    }
}
