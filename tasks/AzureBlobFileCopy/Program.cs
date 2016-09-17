using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace AzureBlobFileCopy
{

    public class ConnectedServiceRelation : PropertyRelation<ProgramOptions, ServiceEndpoint>
    {
        public ConnectedServiceRelation()
            : base(k => k.ConnectedServiceName)
        {

        }
    }

    [ResourceType(TaskInputType = "string")]
    public class ARMListKey : IConsoleReader<ProgramOptions>
    {
        public void OnConsoleParsing(Parser parser, string[] args, ProgramOptions options, PropertyInfo info)
        {
            info.SetValue(options, new ARMListKey() { Id = args[Array.IndexOf(args,"storage")+1] });


        }

        public string Id{get;set;}
      

    }

    [ConnectedServiceRelation(typeof(ConnectedServiceRelation))]
    [EntryPoint("Uploading to $(storage)")]
    public class ProgramOptions
    {

        [Option("source")]        
        public GlobPath Source { get; set; }

        [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        [Required]
        [ArmResourceIdPicker("Microsoft.Storage/storageAccounts", "2015-06-01")]
        [Option("storage")]
      //  [Display(ResourceType =typeof(ARMListKey))]
        public ARMListKey StorageAccount { get; set; }

        [Required]
        [Option("container")]
        public string ContainerName { get; set; }

        [Option("prefix")]
        public string Prefix { get; set; }

    }
    public class Program
    {
        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
#if DEBUG
            args = new[] { "--build" };
#endif
            try
            {

                RunAsync(ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Uploading Data", args), 
                    cancellationTokenSource.Token).Wait();
                
            }
            finally
            {
                runCompleteEvent.Set();
            }

        }

        private static async Task RunAsync(ProgramOptions ops, CancellationToken cannelcationtoken)
        {
            Console.WriteLine(ops.StorageAccount.Id);


        }
    }
}
