using System;
using System.Linq;
using System.Text;
using System.Xml;

namespace GroBuf.Tests.TestTools
{
    public static class XmlHelpers
    {
        public static XmlNode GoToChild(this XmlNode node, string name)
        {
            var xmlNode = TryGoToChild(node, name);
            if (xmlNode != null) return xmlNode;
            throw new FormatException(string.Format("У элемента '{0}' не найдено дочернего '{1}'", node.Name, name));
        }

        public static XmlNode TryGoToChild(this XmlNode node, string name)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                var nodeName = node.ChildNodes[i].Name.Split(':').Last();
                if (string.Compare(nodeName, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return node.ChildNodes[i];
            }
            return null;
        }

        public static T TryGetChildNode<T>(this XmlNode parent, string localName) where T : XmlNode
        {
            return TryGetChildNode<T>(parent, localName, null);
        }

        public static T TryGetChildNode<T>(this XmlNode parent, string localName, string namespaceUri) where T : XmlNode
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (localName.Equals(node.LocalName, StringComparison.OrdinalIgnoreCase)
                    && typeof(T).IsAssignableFrom(node.GetType())
                    && (namespaceUri == null || node.NamespaceURI == namespaceUri))
                    return (T)node;
            }
            return null;
        }

        public static string FormattedOuterXml(this XmlNode node)
        {
            var result = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(result, new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = !node.HasXmlDeclaration()
                });
            node.WriteTo(writer);
            writer.Flush();
            return result.ToString();
        }

        public static string ReformatXml(this string xml)
        {
            return FormattedOuterXml(CreateXml(xml));
        }

        public static XmlDocument CreateXml(string xml)
        {
            return CreateXml(x => x.LoadXml(xml));
        }

        private static bool HasXmlDeclaration(this XmlNode node)
        {
            return node.TryGetChildNode<XmlDeclaration>("xml") != null;
        }

        private static XmlDocument CreateXml(Action<XmlDocument> loadAction)
        {
            var result = new XmlDocument();
            loadAction(result);
            return result;
        }
    }
}