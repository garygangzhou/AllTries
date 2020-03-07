using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SPMLParser.Model
{
    public class SPML_ModifyRequest : SPMLMessage
    {        
        private Dictionary<string, string> data = null;
        public Dictionary<string, string> Data
        {
            get
            {
                LoadXMLString();

                if (data == null)
                    LoadData();

                return data;
            }
        }

        private void LoadData()
        {
            data = new Dictionary<string, string>();
            //find data node first
            XmlNode dataNode = null;
            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                if (node.Name.IndexOf("modification", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dataNode = node;
                    break;
                }
            }
            if (dataNode != null)
            {
                foreach (XmlNode node in dataNode.ChildNodes)
                {
                    string key = node.Attributes["name"].Value;
                    string val = node.FirstChild.InnerText ?? string.Empty;
                    data.Add(key, val);
                }
            }
        }
    }
}
