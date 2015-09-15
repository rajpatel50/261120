using System.Collections.Generic;
using System.Xml;
using Xiap.Framework.Common;
using Xiap.Framework.Validation;

namespace GeniusX.AXA.DPService
{
    public class ITPDocumentDataCollectionPlugin : IDataCollection
    {
        private const string DocumentControlLog = "documentControlLog";
        private const string DocumentControlLogIDArgument = "documentControlLogID";


        public XmlElement GetData(IDictionary<string, object> parameters)
        {
            ArgumentCheck.ArgumentNullCheck(parameters, "parameters");

            long documentControlLogID = 0;
            if (parameters.ContainsKey(DocumentControlLogIDArgument))
            {
                documentControlLogID = (long)parameters[DocumentControlLogIDArgument];
                ArgumentCheck.ArgumentNullCheck(documentControlLogID, DocumentControlLogIDArgument);
            }

            return this.GetData(documentControlLogID);
        }

        private System.Xml.XmlElement GetData(long documentControlLogID)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.InnerXml = "<XIAP><DocumentControlLog><DocumentControlLogID>" + documentControlLogID.ToString() + "</DocumentControlLogID></DocumentControlLog></XIAP>";

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            XmlNodeList xmlNameSpaceList = xmlDocument.SelectNodes(@"//namespace::*[not(. = ../../namespace::*)]");
            foreach (XmlNode node in xmlNameSpaceList)
            {
                if (node.LocalName != node.Name)
                {
                    nsmgr.AddNamespace(node.LocalName, node.Value);
                }
            }

            return xmlDocument.DocumentElement;
        }
    }
}
