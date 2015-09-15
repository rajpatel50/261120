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
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Navigation.AvilabilityBehaviours;
using XIAP.FrontendModules.Common.ClaimService;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimIOLinksNodeAvailabilityBehaviour : ClaimIOLinksNodeAvailabilityBehaviour
    {
        public AXAClaimIOLinksNodeAvailabilityBehaviour(IConverter<DtoBase, long?> productLinkableComponentIDConverter, IConverter<ITransactionController, ClaimModel> transactionControllerConverter) :
            base(productLinkableComponentIDConverter, transactionControllerConverter)
        {
        }

        protected override bool IsAvailableFor(XIAP.FrontendModules.Claims.Model.ClaimModel model, DtoBase parentDto)
        {
            if (base.IsAvailableFor(model, parentDto))
            {
                IClaimDetailData claimDetailData = null;
                claimDetailData = parentDto.Data as ClaimDetailData;
                if (this.InternalIOType == StaticValues.InternalIOType.Vehicle && (claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_AD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPVD))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
