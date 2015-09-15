using System;
using System.Linq;
using System.Collections.Generic;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Infrastructure.NavTree;
using Xiap.Framework.Data;
using Xiap.Metadata.Data.Enums;
using XIAP.FrontendModules.Claims.Model;
using Xiap.Framework.Entity;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimVehiclesChildrenLoadingBehaviour : INodeChildrenLoadingBehaviour
    {
        public void LoadChildren(ITransactionController transactionController, TreeNodeData<ActionKey> node)
        {
            ClaimHeaderDto headerDto = ((ClaimModel)transactionController.Model).HeaderDto;
            List<DtoBase> invToLoad = headerDto.ClaimVehicleInvolvements.Where(x => x.ClaimInsuredObject.ClaimIOVehicles == null).ToList<DtoBase>();

            this.LoadInvolvements(transactionController, node, headerDto.ClaimVehicleInvolvements, invToLoad, Xiap.Metadata.Data.Enums.StaticValues.InternalIOType.Vehicle); 
        }

        protected void LoadInvolvements(ITransactionController transactionController, TreeNodeData<ActionKey> node, List<ClaimInvolvementDto> claimInvolvements, List<DtoBase> invToLoad, StaticValues.InternalIOType type)
        {
            ClaimHeaderDto headerDto = ((ClaimModel)transactionController.Model).HeaderDto;

            if (headerDto.ClaimInvolvementLinks != null)
            {
                claimInvolvements.ForEach(CI =>
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
                             var model = (ClaimModel)transactionController.Model;
                             model.CreateAllInsuredObjectCollection(type);
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
