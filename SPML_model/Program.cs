using SPMLParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SPMLParser
{
    public class Program
    {
        string filefolder = "files";
        readonly string[] files = new string[] {
                "addrequest.xml",
                "addrequest2.xml",
                "deleterequest.xml",
                "modifyrequest.xml",
                "modifyrequest2.xml",
                "resumerequest.xml",
                "suspendrequest.xml"
        };
       
        static void Main(string[] args)
        {
            Program p = new Program();
            p.ProcessMessages();


           // Console.WriteLine("Click Enter to terminate");
          //  Console.ReadLine();
        }

        private List<string> GetMessages()
        {                    
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

        private string[] GetMessages2()
        {
            string[] msgs = new string[files.Length];
            Parallel.For(0, files.Length, (i) => {
                if (File.Exists(Path.Combine(filefolder, files[i])))
                {
                    string requestStr = File.ReadAllText(Path.Combine(filefolder, files[i]));
                    msgs[i] = requestStr;
                }
            });
            return msgs;
        }

        private SPMLMessage[] ProcessMessages()
        {
            //string[] msgs = new string[files.Length];
            SPMLMessage[] spmlMessages = new SPMLMessage[files.Length];
            Parallel.For(0, files.Length, (i) => {
                if (File.Exists(Path.Combine(filefolder, files[i])))
                {
                    string requestStr = File.ReadAllText(Path.Combine(filefolder, files[i]));                   
                    if ( !string.IsNullOrEmpty(requestStr))
                    {
                        SPMLMessage amsg = SPMLMessageParser.Parse2(requestStr);
                        spmlMessages[i] = amsg;
                    }
                }
            });
            return spmlMessages;
        }
    }
}
