using System.Collections.ObjectModel;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using XIAP.Frontend.CommonInfrastructure.Controller.GenericDataSets;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.GenericDataSets;
using XIAP.FrontendModules.Common.MetadataService;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAGenericDataSetController : ClaimGenericDataSetController<ClaimModel>
    {
        private AXAClaimModel _claimModel;
        public AXAGenericDataSetController(ClaimControllerBase<ClaimModel> claimController, AXAClaimModel claimModel, AXAGenericDataItemModel gdiModel, IUnityContainer container, IMetadataClientService metadataService, IGenericDataSetBuilder dataSetBuilder)
           :base(claimController,claimModel,gdiModel,metadataService,dataSetBuilder,container)
        {
            this._claimModel = claimModel;
            this._claimModel.IsBaseFieldsMetadta = false;
            this._claimModel.SetFieldMetadataDelegate(claimController.GetFieldsMetadata_DelegateHandler);
            this.Model.GetParentModel = () => this._claimModel;
        }

        protected override void AddGenericDataItemDialog(AddGenericDataItemEventArgs e, ObservableCollection<GenericDataTypeCodeItem> items)
        {
            ObservableCollection<GenericDataTypeCodeItem> genericDataTypeCodeItem = new ObservableCollection<GenericDataTypeCodeItem>();
            foreach (GenericDataTypeCodeItem genericItem in items)
            {
                if (genericItem.CustomBoolean01 != true)
                {
                    genericDataTypeCodeItem.Add(genericItem);
                }
            }
            
            base.AddGenericDataItemDialog(e, genericDataTypeCodeItem);
        }
    }
}
