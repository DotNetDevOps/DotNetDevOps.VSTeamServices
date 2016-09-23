using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CommandLine;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace AzureBlobFileCopy
{

    public class ConnectedServiceRelation : PropertyRelation<ProgramOptions, ServiceEndpoint>
    {
        public ConnectedServiceRelation()
            : base(k => k.ConnectedServiceName)
        {

        }
    }

    [ResourceType(TaskInputType = "pickList")]
    public class ARMListKey : IConsoleReader<ProgramOptions>, IConsoleExecutor<ProgramOptions>
    {
        public void OnConsoleParsing(Parser parser, string[] args, ProgramOptions options, PropertyInfo info)
        {
            info.SetValue(options, new ARMListKey() { Id = args[Array.IndexOf(args, "--storage") + 1] });


        }

        public void Execute(ProgramOptions options)
        {
            var http = options.ConnectedServiceName.GetAuthorizedHttpClient("https://management.azure.com");

            var keys = http.PostAsync($"https://management.azure.com{Id}/listKeys?api-version=2016-01-01", new StringContent(string.Empty)).GetAwaiter().GetResult();
            var keysObj = JObject.Parse(keys.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Account = new CloudStorageAccount(new StorageCredentials(Id.Split('/').Last(), keysObj.SelectTokens("$.keys[*].value").First().ToString()), true);
        }

        public string Id { get; set; }

        public CloudStorageAccount Account { get; set; }

    }

   // [ConnectedServiceRelation(typeof(ConnectedServiceRelation))]
    [EntryPoint("Uploading to $(storage)")]
    [Group(DisplayName = "Output", isExpanded = true,  Name ="output")]
    public class ProgramOptions
    {

        [Display(ShortName = "source", Name = "Copy Path", Description = "The files that should be copied", ResourceType =typeof(GlobPath))]
        public GlobPath Source { get; set; }

        [Required]
        [Display(Name = "Azure Subscription", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        [Required]
        [ArmResourceIdPicker("Microsoft.Storage/storageAccounts", "2016-01-01")]
        [Display(ShortName ="storage", Name = "Storage Account", Description = "The storage account to copy files to", ResourceType =typeof(ARMListKey))]
        public ARMListKey StorageAccount { get; set; }


        [Display(Name = "Container Name")]
        [Option("container", Required = true)]
        public string ContainerName { get; set; }

        [Display(Name = "Prefix for uploaded data")]
        [Option("prefix")]
        public string Prefix { get; set; }


        [Display(Name = "Storage Container Uri", GroupName = "output")]
        [Option("StorageContainerUri")]
        public string StorageContainerUri { get; set; }

        [Display(Name = "Storage Container SAS token", GroupName = "output")]
        [Option("StorageContainerSASToken")]
        public string StorageContainerSASToken
        {
            get; set;
        }

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

                RunAsync(ConsoleHelper.ParseAndHandleArguments<ProgramOptions>($"Finding and uploading data", args),
                    cancellationTokenSource.Token).Wait();

            }
            finally
            {
                runCompleteEvent.Set();
            }

        }

        private static async Task RunAsync(ProgramOptions ops, CancellationToken cannelcationtoken)
        {

            Console.WriteLine($"Uploading data at {ops.Source} to {ops.StorageAccount.Account.BlobEndpoint} using {ops.Prefix} as prefix in {ops.ContainerName}");
           
            var client = ops.StorageAccount.Account.CreateCloudBlobClient();

            var container = client.GetContainerReference(ops.ContainerName);

            await container.CreateIfNotExistsAsync();

            var actionBlock = new TransformBlock<string,Tuple<string,CloudBlockBlob,TimeSpan>>(async (string file) =>
            {
                var stopWatch = Stopwatch.StartNew();
                using (var fileStream = File.OpenRead(file))
                {
                    var blob = container.GetBlockBlobReference($"{ops.Prefix}/{file.Substring(ops.Source.Root.Length)}".TrimStart('/'));
                    blob.Properties.ContentType = Constants.GetContentType(file);

                    using (var writeable = await blob.OpenWriteAsync())
                    {
                        await fileStream.CopyToAsync(writeable);
                    }
                    return new Tuple<string,CloudBlockBlob,TimeSpan>(file,blob,stopWatch.Elapsed);
                }

            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 64 });

            var completed = new ActionBlock<Tuple<string, CloudBlockBlob,TimeSpan>>((blob) =>
            {
                Console.WriteLine($"Uploaded {blob.Item1} to {blob.Item2.Name} completed in {blob.Item3}");

            });

            actionBlock.LinkTo(completed,new DataflowLinkOptions { PropagateCompletion = true });
            foreach(var file in ops.Source.MatchedFiles())
            {
                await actionBlock.SendAsync(file);  
            }

            actionBlock.Complete();

            await completed.Completion;


            if (!string.IsNullOrEmpty(ops.StorageContainerUri))
            {
                TaskHelper.SetVariable(ops.StorageContainerUri, container.Uri.ToString());

            }
            if (!string.IsNullOrEmpty(ops.StorageContainerSASToken))
            {
                TaskHelper.SetVariable(ops.StorageContainerSASToken, container.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(2),
                    Permissions = SharedAccessBlobPermissions.Add | SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Delete | SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
                }),
                     true);
            }

        }
    }
}
