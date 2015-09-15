using System;
using System.Linq;
using Xiap.Framework.Common.Product;
using Xiap.Framework.Logging;
using Xiap.Framework.Warmup;
using Xiap.Metadata.BusinessComponent;

namespace GeniusX.AXA.Underwriting.BusinessLogic
{
    public class AXAUWWarmupPlugin : IWarmupTask
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string _Name = "AXAUWWarmupPlugin";

        public string Name
        {
            get { return this._Name; }
        }

        /// <summary>
        /// Warmup scripts for ProductWordings, ProductSections, ProductEvents, ProductNameInvolvements and ProductDocuments
        /// </summary>
        /// <returns>true if successful</returns>
        public bool Process()
        {
            try
            {
                using (MetadataEntities me = MetadataEntitiesFactory.GetMetadataEntities())
                {
                    var productversionids = me.ProductUwDefinition.Select(x => x.ProductVersionID);
                    foreach (long productVersionID in productversionids)
                    {
                        ProductService.GetProductWordingQuery().GetProductWordings(productVersionID);
                        ProductService.GetProductSectionQuery().GetProductSections(productVersionID);
                        ProductService.GetProductEventQuery().GetProductEvents(productVersionID);
                        ProductService.GetProductNameInvolvementQuery().GetProductNameInvolvements(productVersionID);
                        ProductService.GetProductDocumentQuery().GetProductDocuments(productVersionID);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);
                _Logger.Info(ex.StackTrace);
                return false;
            }
        }
    }
}
