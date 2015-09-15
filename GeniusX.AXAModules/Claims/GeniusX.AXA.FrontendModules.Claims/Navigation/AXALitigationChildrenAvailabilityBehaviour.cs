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
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.ClaimsService;
using XIAP.FrontendModules.Infrastructure.NavTree;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXALitigationChildrenAvailabilityBehaviour: INodeAvailabilityBehaviour 
    {
        public bool IsAvailable(XIAP.Frontend.Infrastructure.ITransactionController transactionController, XIAP.FrontendModules.Infrastructure.NavTree.TreeStructureStore definition, Xiap.Framework.Data.DtoBase parentDto)
        {
            IClaimLitigationData cld = null;
            cld = parentDto.Data as ClaimLitigationData;

            if (definition.Node == "ClaimLitigationMainDetails" && (cld.LitigationType == AXAClaimConstants.LITIGATIONTYPE_LIT || cld.LitigationType == AXAClaimConstants.LITIGATIONTYPE_OTH))
            {
                return true;
            }

            return false;
        }
    }
}
