using SPMLParser.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace SPML_model
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            Program p = new Program();
            //read messages from text files
            List<string> messages = p.GetMessages();
            log.Info($"Find {messages.Count} messages");

            List<SPMLMessage> spmlMessages = new List<SPMLMessage>();
            foreach(string msg in messages)
            {
                SPMLMessage amsg = SPMLMessageParser.Parse2(msg);
                spmlMessages.Add( amsg );

                //if ( amsg is SPML_AddRequest)
                //{
                //    SPML_AddRequest ar = amsg as SPML_AddRequest;
                //    string psoid = ar.PSOID;
                //    Dictionary<string, string> requestdata = ar.Data;
                //}                
            }

           // Console.WriteLine("Click Enter to terminate");
          //  Console.ReadLine();
        }

        private List<string> GetMessages()
        {
            string filefolder = "files";
            List<String> files = new List<string> {
                "addrequest.xml",
                "addrequest2.xml",
                "deleterequest.xml", 
                "modifyrequest.xml", 
                "modifyrequest2.xml",
                "resumerequest.xml", 
                "suspendrequest.xml"
            };

            List<string> msgs = new List<string>();
            foreach (string file in files)
            {
                if (File.Exists(Path.Combine(filefolder, file)))
                {
                    string requestStr = File.ReadAllText(Path.Combine(filefolder, file));
                    msgs.Add(requestStr);
                }
            }
            return msgs;
        }       
    }
}
