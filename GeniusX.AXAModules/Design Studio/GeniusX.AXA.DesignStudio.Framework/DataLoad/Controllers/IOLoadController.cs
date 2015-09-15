using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Practices.Unity;
using Xiap.DesignStudio.Framework.DataLoad.Controllers;
using Xiap.DesignStudio.Framework.DataLoad.Models;
using Xiap.DesignStudio.Modules.GeneralAdministration.DataLoad;
using Xiap.Framework.Logging;

namespace GeniusX.AXA.DesignStudio.Framework.DataLoad.Controllers
{
    public class IOLoadController : BaseDataLoadController
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
       private IUnityContainer Container;
        public IOLoadController(BaseDataLoadModel dataloadModel, IUnityContainer container) :base(dataloadModel, container)
        {
            this.Container = container;
            this.DataLoadModel.OnComplete += new EventHandler(this.DataLoadModel_OnComplete);
        }

      private void DataLoadModel_OnComplete(object sender, EventArgs e)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            XmlElement reqElement = null;
            XmlDocument x = new XmlDocument();

            using (XmlReader reader = XmlReader.Create(this.DataLoadModel.FilePath, settings))
            {
                reader.MoveToContent();

                if (reader.Name == String.Empty)
                {
                    logger.Info(DataLoadConstants.STR_NODE_NOT_PRESENT);
                    return;                    
                }

                // Create root node and associate attributes
                reqElement = x.CreateElement(reader.Name);
                int i = 0;
                while (i < reader.AttributeCount && reader.HasAttributes)
                {
                    reader.MoveToAttribute(i);
                    reqElement.SetAttribute(reader.Name, reader.Value);
                    i++;
                }

                // Create request message with list of registration Ids
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == DataLoadConstants.ELEMENT_REGISTRATIONID)
                    {
                        XElement xelement = this.CreateVehicleInfoChildElement(reader);
                        XmlElement item = x.ReadNode(xelement.CreateReader()) as XmlElement;
                        if (item != null)
                        {
                            reqElement.AppendChild(item);
                        }
                    }
                }

                XmlElement response = this.InvokeXmphProcessData(reqElement);
            }
        }


        private XElement CreateVehicleInfoChildElement(XmlReader reader)
        {
            XElement vehInfoElement = new XElement(DataLoadConstants.ELEMENT_VEHILCEINFO);
            XElement regIDElement = new XElement(XDocument.Parse(reader.ReadOuterXml()).Root);
            vehInfoElement.Add(regIDElement);
            return vehInfoElement;
        }

        private XmlElement InvokeXmphProcessData(XmlElement updateRequestElement)
        {
            logger.Info(string.Format(DataLoadConstants.STR_XMPH_REQUEST));
            
            string sourceSystem = this.GetSourceSystem();
            string targetSystem = this.GetTargetSystem();
            string transaction = DataLoadConstants.TRANSACTION_SYNCHRONIZE_VEHICLE_DATA;

            logger.Info(string.Format(DataLoadConstants.STR_SOURCE_SYSTEM,sourceSystem));
            logger.Info(string.Format(DataLoadConstants.STR_TARGET_SYSTEM, targetSystem));
            logger.Info(string.Format(DataLoadConstants.STR_TRANSACTION, transaction));

            List<string> parameters = new List<string>();
            parameters.Add(sourceSystem);
            parameters.Add(targetSystem);
            parameters.Add(transaction);

            XmlElement response = null;
            

            // Send/Recieve response from XMPH
            IProcessMessage processMesage = this.Container.Resolve<IProcessMessage>(DataLoadConstants.STR_PROCESS_MESSAGE);
            try
            {
                response = processMesage.ProcessData(updateRequestElement, parameters.ToArray());

                if (response.InnerText == DataLoadConstants.STR_FAILURE)
                {
                    throw new Exception(DataLoadConstants.ERRORS_ENCOUNTERED);
                }                
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Error Message :{0}    {1}", ex.Message, ex.InnerException));
                throw ex;
            }

            return response;
        }
    }
}
