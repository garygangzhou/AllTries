using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SPMLParser.Model
{
    public class SPMLMessage
    {
        protected XmlDocument document;
        public string Message { get; set; }  

        private string psoID;
        public string PSOID
        {
            get
            {
                LoadXMLString();
                if (psoID == null)
                {
                    psoID = GetPSOID();
                }
                return psoID;
            }
        }       

        protected void LoadXMLString()
        {
            if (document == null)
            {
                document = new XmlDocument();
                document.LoadXml(Message);
            }
        }

        private  string GetPSOID()
        {
            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                if (node.Name.IndexOf("psoID", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return node.Attributes["ID"].Value;
                }
            }
            //not found
            return string.Empty;
        }

    }
}
