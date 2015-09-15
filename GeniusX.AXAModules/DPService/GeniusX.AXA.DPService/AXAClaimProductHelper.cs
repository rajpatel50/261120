using System.Data;
using System.Linq;
using System.Xml;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.Configuration;
using Xiap.Framework.Logging;
using Xiap.Metadata.BusinessComponent;

namespace GeniusX.AXA.DPService
{
    /// <summary>
    /// This class gets the claim document URL for uploading.
    /// </summary>
    public class AXAClaimProductHelper
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// This method return the product code based on the claimreference.
        /// </summary>
        /// <param name="claimReference"> Claim Reference</param>
        /// <returns>Product code </returns>
        public static string GetProductCode(string claimReference)
        {
            string productCode = string.Empty;
            using (ClaimsEntities claimsEntities = ClaimsEntitiesFactory.GetClaimsEntities())
            {
                long? claimProductVersionId = (from row in claimsEntities.ClaimHeader
                                               where row.ClaimReference.Equals(claimReference)
                                               select row.ClaimProductVersionID).FirstOrDefault();

                productCode = ProductHelper.GetProductCode((long)claimProductVersionId);
                return productCode;
            }
        }

        /// <summary>
        /// This method return the claim document URL.
        /// </summary>
        /// <param name="productCode">Product code </param>
        /// <returns> URl of the document</returns>
        public static string GetProductFolderURL(string productCode)
        {
            string URL = string.Empty;
            XmlNode processConfigs = ConfigurationFactory.ConfigurationManager.GetSection<XmlNode>("xiap/custom/sharepointConfiguration");
            if (processConfigs == null)
            {
                throw new ObjectNotFoundException("Can not find a sharepointConfiguration section in configuration file");
            }

            URL = GetURL(productCode,processConfigs);
           
            return URL;
        }

        private static string GetURL(string productCode,XmlNode processConfigs)
        {
            XmlNode rootNode = processConfigs.SelectSingleNode("//documentUploadSites");
            if (rootNode == null)
            {
                return null;
            }

            string path = "//productCode";
            XmlNodeList nodes = rootNode.SelectNodes(path);
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    if (node.InnerText != null && node.InnerText == productCode)
                    {
                        if (node.ParentNode != null && node.ParentNode.ParentNode != null)
                        {
                            XmlNode urlNode = node.SelectSingleNode("//url");
                            if (urlNode != null && urlNode.InnerText!=null)
                            {
                                return urlNode.InnerText;
                            }
                        }

                        break;
                    }
                }
            }

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("Claim document Url cannot be found for product {0}", productCode));
            }

            if (rootNode != null && rootNode.Attributes["defaultURL"] != null)
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("Claim defaultURL {0}", rootNode.Attributes["defaultURL"].Value.ToString()));
                }

                return rootNode.Attributes["defaultURL"].Value.ToString();
            }

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug("returned null");
            }

            return null;
        }
    }
}
