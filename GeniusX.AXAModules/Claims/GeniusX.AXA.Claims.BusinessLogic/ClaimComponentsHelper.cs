using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Data;
using Xiap.Metadata.BusinessComponent;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    internal static class ClaimComponentsHelper
    {
        /// <summary>
        /// Create client specific GenericDataSet
        /// </summary>
        /// <param name="component">Claim Generic DataSet Container</param>
        /// <param name="clientCode">Client Code</param>
        internal static void CreateClientSpecificGenericDataSet(IBusinessComponent component, string clientCode)
        {
            // Retrieve ProductGD definition header
            IClaimGenericDataSetContainer genericDataSetContainer = (IClaimGenericDataSetContainer)component;
            ProductGDDefinitionHeader genericDataDefHeader = genericDataSetContainer.GetProductGDDefinitionHeader();
            if (genericDataDefHeader != null)
            {
                ProductGDDefinitionDetail genericDataDefDetail = null;
                genericDataDefDetail =  genericDataDefHeader.ProductGDDefinitionDetails.FirstOrDefault((gd) =>
                        {
                            var gdtVersion = ObjectFactory.Resolve<IMetadataQuery>().GetGenericDataTypeVersion(gd.ProductGDDefinitionDetailID, genericDataSetContainer.GDSParentStartDate);

                            // CustomCode01 ClientCode01
                            if (gdtVersion != null && gdtVersion.GenericDataTypeComponent.CustomCode01 == clientCode)   // UI Label = Client Code 1; ClientCodes
                            {
                                return true;
                            }

                            return false;
                        });

                // Create Generic Data Set and add Generic Data Item
                if (genericDataDefDetail != null)
                {
                    IGenericDataSet genericDataSet = genericDataSetContainer.GetGenericDataSet();
                    if (genericDataSet == null)
                    {
                        genericDataSet = genericDataSetContainer.CreateGenericDataSet();
                    }

                    genericDataSet.AddGenericDataItem(genericDataDefDetail.ProductGDDefinitionDetailID, genericDataSetContainer.GDSParentStartDate);
                }
            }
        }

        /// <summary>
        /// Delete existing client specific code
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        internal static void DeleteExistingClientSpecificCode(ClaimHeader claimHeader)
        {
            ClaimGenericDataSet headerDataSet = claimHeader.GenericDataSet;
            
            DeleteClientSpecificGDI(headerDataSet);
        }

        /// <summary>
        /// delete client specific GenericDataItems
        /// </summary>
        /// <param name="headerDataSet">Claim Generic DataSet</param>
        private static void DeleteClientSpecificGDI(ClaimGenericDataSet headerDataSet)
        {
            if (headerDataSet != null && headerDataSet.GenericDataItems != null && headerDataSet.GenericDataItems.Count > 0)
            {
                // CustomBoolean01=Client-specClm GDT?
                var clientCodes = headerDataSet.GenericDataItems.Where(gdi => gdi.CustomBoolean01 == true);   // UI Label = Client Specific Claim Generic Data Type; GDT is Deductible Policy Reference
                foreach (var clientCode in clientCodes)
                {
                    ClaimGenericDataItem genericDataItem = (ClaimGenericDataItem)clientCode;
                    Xiap.Claims.BusinessLogic.ClaimsBusinessLogicHelper.DeleteComponent(genericDataItem.Context, genericDataItem);
                    clientCode.Context.RegisterComponentChange(genericDataItem.DataId, BusinessDataState.Deleted);
                }
            }
        }
    }
}
