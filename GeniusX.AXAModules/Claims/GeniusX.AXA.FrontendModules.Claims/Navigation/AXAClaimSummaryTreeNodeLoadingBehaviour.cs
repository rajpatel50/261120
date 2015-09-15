using System;
using System.Net;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Infrastructure.NavTree;
using XIAP.FrontendModules.Claims.Enumerations;
using XIAP.FrontendModules.Claims.Model;
using Xiap.Framework.Data;
using Xiap.Framework.Entity;
using System.Collections.Generic;
using Xiap.Framework.Validation;
using XIAP.FrontendModules.Common.ClaimService;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimSummaryTreeNodeLoadingBehaviour : INodeChildrenLoadingBehaviour
    {
        public void LoadChildren(XIAP.Frontend.Infrastructure.ITransactionController transactionController, TreeNodeData<ActionKey> node)
        {
            var headerDto = (ClaimHeaderDto)node.DataObject;
            List<DtoBase> toLoad = new List<DtoBase>();
            List<DtoBase> invToLoad = headerDto.ClaimVehicleInvolvements.Where(x => x.ClaimInsuredObject.ClaimIOVehicles == null).ToList<DtoBase>();
           
            if (headerDto.ClaimInvolvementLinks != null)
            {
                headerDto.ClaimVehicleInvolvements.ForEach(CI =>
                {
                    var involvementLinkFrom = headerDto.ClaimInvolvementLinks.Where(CIL => ((ClaimInvolvementLinkData)CIL.Data).ClaimInvolvementFromDataId == CI.Data.DataId);
                    involvementLinkFrom.ForEach(y =>
                    {
                        var involvement = headerDto.ClaimInvolvements.Where(z => z.Data.DataId == ((ClaimInvolvementLinkData)y.Data).ClaimInvolvementToDataId);
                        invToLoad.AddRange(involvement);
                    });


                    var involvementLinkTo = headerDto.ClaimInvolvementLinks.Where(CIL => ((ClaimInvolvementLinkData)CIL.Data).ClaimInvolvementToDataId == CI.Data.DataId);
                    involvementLinkTo.ForEach(y =>
                    {
                        var involvement = headerDto.ClaimInvolvements.Where(z => z.Data.DataId == ((ClaimInvolvementLinkData)y.Data).ClaimInvolvementFromDataId);
                        invToLoad.AddRange(involvement);
                    });
                });
            }

            int invToLoadCount = invToLoad.Distinct().Count();

            int count = 0;
            if (invToLoad.Distinct().Any())
            {
                transactionController.LoadLiteData(
                   Guid.Empty,
                   RetrievalType.WithChildHierarchy,
                   invToLoad.Distinct(),
                   NavigationType.None,
                   null,
                    r =>
                    {
                        if (++count == invToLoadCount)
                        {
                            node.IsLoaded = true;
                        }
                    },
                   true,
                   BusinessDataVariant.Full);
            }
            else
            {
                node.IsLoaded = true;
            }
        }
    }
}
