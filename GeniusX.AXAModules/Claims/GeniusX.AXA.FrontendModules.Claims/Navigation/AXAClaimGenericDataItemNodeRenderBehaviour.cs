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
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.FrontendModules.Claims.Behaviour;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.GenericDataSets;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimGenericDataItemNodeRenderBehaviour :INodeRenderBehaviour, IDisposable
    {
        private IClaimPropertyChangedHandler<ClaimGenericDataItemDto> handler;
        public AXAClaimGenericDataItemNodeRenderBehaviour(IClaimPropertyChangedHandler<ClaimGenericDataItemDto> handler)
        {
            this.handler = handler;
        }

        public void SetRenderContext(IViewController controller, object context)
        {
            ClaimGenericDataItemDto genericDataItemDto = (ClaimGenericDataItemDto)context;
            ClaimModel claimModel = (ClaimModel)controller.Model;
            claimModel.SelectedDto = genericDataItemDto;
            this.handler.ClaimController = controller;
            this.handler.AttachHandler(genericDataItemDto);
            ClaimGenericDataItemData genericDataItem = (ClaimGenericDataItemData)genericDataItemDto.GenericDataItemData;
            claimModel.GenericDataItemModel.SelectedGenericDataItemDto = genericDataItemDto;
            claimModel.GenericDataItemModel.GenericDataSetContainer = (IGenericDataSetContainer)genericDataItem.ParentDto.Data.ParentDto;
        }

        ////private void RefreshGenericDataItems(IGenericDataSetContainer container)
        ////{
        ////    int Loaded = 0;
        ////    if (container.GenericDataSetDto != null)
        ////    {
        ////        container.GenericDataSetDto.GenericDataItemsDto.ForEach(
        ////            dto =>
        ////            {
        ////                Loaded++;
        ////                this.claimModel.GenericDataItemModel.RefreshGenericDataItems(container, dto);
        ////                if (Loaded == container.GenericDataSetDto.GenericDataItemsDto.Count)
        ////                {
        ////                    this.claimModel.InvokeCollectionChanged("GenericDataItems", Guid.Empty);
        ////                }
        ////            });
        ////    }
        ////}

        public void Dispose()
        {
            this.handler.DetachHandler();
        }
    }
}
