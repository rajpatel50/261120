namespace Xiap.DataMigration.GeniusInterface.AXACS
{
    using System.Xml;
    using System.Xml.Linq;

    public static class XElementExtensions
    {
        public static XmlNode ToXmlNode(this XElement source)
        {
            var doc = new XmlDocument();
            doc.LoadXml(source.ToString());
            return doc.FirstChild;
        }
    }
}
