using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

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
            Id = args[Array.IndexOf(args, "--storage") + 1];
        }

        public void Execute(ProgramOptions options)
        {
            var http = options.ConnectedServiceName.GetAuthorizedHttpClient("https://management.azure.com/");

            var keys = http.PostAsync($"https://management.azure.com{Id}/listKeys?api-version=2016-01-01", new StringContent(string.Empty)).GetAwaiter().GetResult();
            var keysObj = JObject.Parse(keys.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Account = new CloudStorageAccount(new StorageCredentials(Id.Split('/').Last(), keysObj.SelectTokens("$.keys[*].value").First().ToString()), true);
        }

        public string Id { get; set; }

        public CloudStorageAccount Account { get; set; }

    }

    [ConnectedServiceRelation(typeof(ConnectedServiceRelation))]
    [EntryPoint("Uploading to $(storage)")]
    [Group(DisplayName = "Output", isExpanded = true, Name = "output")]
    public class ProgramOptions
    {

        [Display(ShortName = "source", Name = "Copy Path", Description = "The files that should be copied", ResourceType = typeof(GlobPath))]
        public GlobPath Source { get; set; }

        [Required]
        [Display(Name = "Azure Subscription", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        [Required]
        [ArmResourceIdPicker("Microsoft.Storage/storageAccounts", "2016-01-01")]
        [Display(ShortName = "storage", Name = "Storage Account", Description = "The storage account to copy files to", ResourceType = typeof(ARMListKey))]
        public ARMListKey StorageAccount { get; set; }


        [Display(Name = "Container Name")]
        [Option("container", Required = true)]
        public string ContainerName { get; set; }

        [Display(Name = "Prefix for uploaded data")]
        [Option("prefix")]
        public string Prefix { get; set; }

        [Display(Name = "Fail if files Exists")]
        [DefaultValue(true)]
        [Option("failOnExists")]
        public bool FailIfFilesExist { get; set; }

        [Display(Name = "Storage Container Uri", GroupName = "output")]
        [Option("StorageContainerUri")]
        public string StorageContainerUri { get; set; }

        [Display(Name = "Storage Container SAS token", GroupName = "output")]
        [Option("StorageContainerSASToken")]
        public string StorageContainerSASToken
        {
            get; set;
        }


        [Display(Name = "Verbose", Description = "Write out each file thats uploaded")]
        [Option("Verbose")]
        public bool Verbose { get; set; }
    }
    public class Program
    {
        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
#if DEBUG
     //       args = new[] { "--build" };
#endif
            ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DefaultConnectionLimit = ServicePointManager.DefaultPersistentConnectionLimit * 100;

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

            if (ops.FailIfFilesExist)
            {
                var uploads = ops.Source.MatchedFiles()
                    .Select(file => Path.Combine(ops.Prefix, file.Substring(ops.Source.Root.Length).TrimStart('/', '\\')).Replace("\\", "/"))
                    .ToLookup(k=>k);

                foreach(var file in container.ListBlobs(ops.Prefix, true).OfType<CloudBlockBlob>().Select(b => b.Name))
                {
                    if (uploads.Contains(file))
                    {
                        Console.WriteLine("##vso[task.logissue type=error] File Exists: " + file);
                        throw new Exception("File exists: " + file);
                    }
                }

                
            }

            

            var actionBlock = new TransformBlock<string, Tuple<string, CloudBlockBlob, TimeSpan>>(async (string file) =>
               {
                   var filestopWatch = Stopwatch.StartNew();
                   using (var fileStream = File.OpenRead(file))
                   {
                       var blob = container.GetBlockBlobReference(Path.Combine(ops.Prefix,file.Substring(ops.Source.Root.Length).TrimStart('/','\\')).Replace("\\","/"));
                       blob.Properties.ContentType = Constants.GetContentType(file);

                       using (var writeable = await blob.OpenWriteAsync())
                       {
                           await fileStream.CopyToAsync(writeable);
                       }
                       return new Tuple<string, CloudBlockBlob, TimeSpan>(file, blob, filestopWatch.Elapsed);
                   }

               }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 64 });

            var i = 0;
            var completed = new ActionBlock<Tuple<string, CloudBlockBlob, TimeSpan>>((blob) =>
             {
                 if (ops.Verbose)
                 {
                     Console.WriteLine($"Uploaded {blob.Item1} to {blob.Item2.Name} completed in {blob.Item3}");
                 }

                 Interlocked.Increment(ref i);
             });

            actionBlock.LinkTo(completed, new DataflowLinkOptions { PropagateCompletion = true });
            var stopWatch = Stopwatch.StartNew(); 
            foreach (var file in ops.Source.MatchedFiles())
            {
                
                await actionBlock.SendAsync(file);
            }

            actionBlock.Complete();

            await completed.Completion;

            Console.WriteLine($"Uploaded {i} files to {container.Name}{ops.Prefix} in {stopWatch.Elapsed}");


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
