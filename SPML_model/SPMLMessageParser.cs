using System;
using System.Text.RegularExpressions;
using System.Xml;
using SPMLParser.Model;

namespace SPMLParser
{
    public static class SPMLMessageParser
    {
        readonly static string regExp = @"[^a-zA-Z]*([a-zA-Z]+Request$)";
        
        public static SPMLMessage Parse2(string messageXMLstring)
        {
            //load xml message
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(messageXMLstring.Trim());
            XmlNode root = doc.DocumentElement;

            Regex reg = new Regex(regExp);
            MatchCollection matches = reg.Matches(root.Name);

            if ( matches.Count == 0 || matches[0].Groups.Count <= 1 )
            {
                return new SPML_UnknownRequest() { Message = messageXMLstring };
            }
            string requestName = matches[0].Groups[1].Value;
            Logger.Log.Debug($"node name: {root.Name}, requesttype: {requestName}");

            switch (requestName)
            {
                case "addRequest":
                    return new SPML_AddRequest() { Message = messageXMLstring };
                case "modifyRequest":
                    return new SPML_ModifyRequest() { Message = messageXMLstring };
                case "suspendRequest":
                    return new SPML_SuspendRequest() { Message = messageXMLstring };
                case "deleteRequest":
                    return new SPML_DeleteRequest() { Message = messageXMLstring };
                case "resumeRequest":
                    return new SPML_ResumeRequest() { Message = messageXMLstring };
                default:
                    return new SPML_UnknownRequest() { Message = messageXMLstring };
            }
        }

        public static SPMLMessage Parse(string messageXMLstring)
        {
            //load xml message
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(messageXMLstring.Trim());

            XmlNode root = doc.DocumentElement;
            if ( root.Name.IndexOf("addRequest", StringComparison.OrdinalIgnoreCase) >= 0) {
                return new SPML_AddRequest() { Message = messageXMLstring };
            }
            else if ( root.Name.IndexOf("modifyRequest", StringComparison.OrdinalIgnoreCase) >= 0) {
                return new SPML_ModifyRequest() { Message = messageXMLstring };
            }
            else if ( root.Name.IndexOf("suspendRequest", StringComparison.OrdinalIgnoreCase) >= 0) {
                return new SPML_SuspendRequest() { Message = messageXMLstring };
            }
            else if ( root.Name.IndexOf("deleteRequest", StringComparison.OrdinalIgnoreCase) >= 0) {
                return new SPML_DeleteRequest() { Message = messageXMLstring };
            }
            else if ( root.Name.IndexOf("resumeRequest", StringComparison.OrdinalIgnoreCase) >= 0) {
                return new SPML_ResumeRequest() { Message = messageXMLstring };
            }
            else {
                return new SPML_UnknownRequest() { Message = messageXMLstring };
            }            
        }        
    }
}
