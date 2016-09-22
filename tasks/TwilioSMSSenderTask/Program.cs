using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using Twilio;

namespace TwilioSMSSenderTask
{

    [EntryPoint("Sending sms to $(ReceiverNumber)")]
    public class ProgramOptions
    {
        [Option("AccountSid")]
        public string AccountSid { get; set; }
        [Option("AuthToken")]
        public string AuthToken { get; set; }
        [Option("Message")]
        public string Message { get; set; }
        [Option("ReceiverNumber")]
        public string ReceiverNumber { get; set; }
        [Option("SenderNumber")]
        public string SenderNumber { get; set; }
    }
    public class Program
    {
        
        static void Main(string[] args)
        {
#if DEBUG
            args = new[] { "--build" }; 
#endif

            var ops = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Sending SMS", args);
            var client = new TwilioRestClient(ops.AccountSid, ops.AuthToken);

            client.SendMessage(
                   ops.SenderNumber, // From number, must be an SMS-enabled Twilio number
                   ops.ReceiverNumber,     // To number, if using Sandbox see note above
                                           // message content
                  "Hello Azure Group" // ops.Message  
            );

            Console.WriteLine(string.Format("Sent message to {0}", ops.ReceiverNumber));
        }


    }
}
