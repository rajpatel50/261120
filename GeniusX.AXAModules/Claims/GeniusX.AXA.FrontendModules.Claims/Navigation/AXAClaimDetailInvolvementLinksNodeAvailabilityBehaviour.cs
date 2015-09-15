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
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Navigation.AvilabilityBehaviours;
using XIAP.FrontendModules.Common.ClaimService;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimDetailInvolvementLinksNodeAvailabilityBehaviour : InvolvementLinksNodeAvailabilityBehaviour
    {
        public AXAClaimDetailInvolvementLinksNodeAvailabilityBehaviour(IConverter<DtoBase, long?> productLinkableComponentIDConverter, IConverter<ITransactionController, ClaimModel> transactionControllerConverter) :
            base(productLinkableComponentIDConverter, transactionControllerConverter)
        {
        }

        protected override bool IsAvailableFor(ClaimModel model, DtoBase parentDto)
        {
            if (base.IsAvailableFor(model, parentDto))
            {
                IClaimDetailData claimDetailData = null;
                claimDetailData = parentDto.Data as ClaimDetailData;

                if (claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_AD && this.LinkableComponentType == StaticValues.LinkableComponentType.Recovery)
                {
                    return true;
                }

                if ((claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPVD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPPD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPI || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_LIA) && this.LinkableComponentType == StaticValues.LinkableComponentType.Litigation)
                {
                    return true;
                }
            }

            return false ;
        }
    }
}
