using System;
using System.Collections.Generic;
using System.Text;
using K2Processes.K2SmartObjectService.Core;
using ServiceReferenceClient.DataService;
using System.Xml.Linq;
using K2Processes.K2SmartObjectService.Logging;

namespace GeniusX.AXA.ServiceObject
{
    public class AxaServiceObject : BaseServiceObject
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        public void GetAuthorisationDestination()
        {
            SourceCode.SmartObjects.Services.ServiceSDK.Objects.ServiceObject serviceObject = Service.ServiceObjects[0];
            string dataCollector = GetMandatoryServiceObjectProperty(serviceObject, "DataCollector").ToString();

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("claimTransactionHeaderId", GetMandatoryServiceObjectProperty(serviceObject, "ClaimTransactionHeaderID"));
            properties.Add("creatorUserIdentity", GetMandatoryServiceObjectProperty(serviceObject, "CurrentUserIdentity"));

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("GetAuthorisationDestination({0}, {1})", properties["claimTransactionHeaderId"], properties["creatorUserIdentity"]));
            }

            DataServiceClient client = null;
            try
            {
                client = new DataServiceClient();
                XElement response = client.GetData(dataCollector, properties);
                string destinationType = response.Attribute("DestinationType").Value;
                string destinationName = response.Attribute("DestinationName").Value;

                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("GetAuthorisationDestination({0}, {1}) => name: {2}, type: {3}", properties["claimTransactionHeaderId"], properties["creatorUserIdentity"], destinationType, destinationName));
                }

                serviceObject.Properties.InitResultTable();
                serviceObject.Properties["DestinationType"].Value = destinationType;
                serviceObject.Properties["DestinationName"].Value = destinationName;
                serviceObject.Properties.BindPropertiesToResultTable();
            }
            finally
            {
                if (client != null && client.State == System.ServiceModel.CommunicationState.Opened)
                {
                    client.Close();
                }
            }
        }
    }
}
