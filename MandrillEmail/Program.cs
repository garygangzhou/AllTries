using Mandrill;
using Mandrill.Models;
using Mandrill.Requests.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MandrillEmail
{
    class Program
    {
        private static string apiKey = System.IO.File.ReadAllText("key.txt");
        public static async System.Threading.Tasks.Task Main(string[] args)
        {
            //MandrillApi api = new MandrillApi(apiKey);
            //UserInfo info = await api.UserInfo();
            //Console.WriteLine(info.Username);

            SendTestingEmail();

            Console.WriteLine("done");
            Console.ReadLine();
        }
        
        static async void SendTestingEmail()
        {
            EmailMessage email = new EmailMessage
            {
                FromEmail = "gary.zhou@cancercare.on.ca",
                FromName = "Gary Zhou",
                Subject = "Welcome Mandrill API testing.",                
                Text = "Hello there, a few try first.",               
                To = new List<EmailAddress>()
                {
                  new EmailAddress("gary.zhou@ontariohealth.ca")
                }
            };          

            IMandrillApi api = new MandrillApi(apiKey);            
            UserInfo info = await api.UserInfo();
            Debug.WriteLine($"API user info: {info.Username}, All Time Sent: {info.Stats.AllTime.Sent}");

            SendMessageRequest emailRequest = new SendMessageRequest(email);
            List<EmailResult> results = await api.SendMessage (emailRequest);

            foreach(var result in results)
            {
                Debug.WriteLine($"result: {result.Status}");
            }
        }
    }
}
