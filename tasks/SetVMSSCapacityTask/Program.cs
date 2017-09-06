using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;

using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyInformationalVersion("1.0.1")]
[assembly: AssemblyTitle("VMSS : Update Capacity")]
[assembly: AssemblyDescription("Update the Capacity on a VMSS")]
[assembly: AssemblyConfiguration("Utility")]
[assembly: AssemblyCompany("S-Innovations.Net v/Poul K. Sørensen")]
[assembly: AssemblyProduct("SetVMSSCapacityTask")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d16f8aa7-ab1a-4a13-b778-77e425e74fe1")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

namespace SetVMSSCapacityTask
{

    [DataContract]
    public class ArmError
    {
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string Code { get; set; }
    }
    [DataContract]
    public class ArmErrorBase
    {
        [DataMember]
        public ArmError Error { get; set; }
    }

    public static class HttpClientExtensions
    {


        public static async Task<T> As<T>(this Task<HttpResponseMessage> messageTask)
        {
            var message = await messageTask;

            if (!typeof(ArmErrorBase).IsAssignableFrom(typeof(T)) && !message.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await message.Content.ReadAsStringAsync());
            }

            using (var stream = await message.Content.ReadAsStreamAsync())
            {
                using (var sr = new JsonTextReader(new StreamReader(stream)))
                {
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    });

                    return serializer.Deserialize<T>(sr);
                }
            }
        }
    }

    public class ArmClient
    {


        protected HttpClient Client { get; set; }
        public ArmClient(AuthenticationHeaderValue authorization)
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Authorization = authorization;
        }

        public ArmClient(string accessToken) : this(new AuthenticationHeaderValue("bearer", accessToken))
        {

        }

        public Task<T> ListKeysAsync<T>(string resourceId, string apiVersion)
        {
            var resourceUrl = $"https://management.azure.com/{resourceId.Trim('/')}/listkeys?api-version={apiVersion}";

            return Client.PostAsync(resourceUrl, new StringContent(string.Empty))
                .As<T>();
        }

        public Task<T> PatchAsync<T>(string resourceId, T value, string apiVersion)
        {
            var resourceUrl = $"https://management.azure.com/{resourceId.Trim('/')}?api-version={apiVersion}";
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), resourceUrl);
            var valuestr = JsonConvert.SerializeObject(value);
            request.Content = new StringContent(valuestr, Encoding.UTF8, "application/json");

            return Client.SendAsync(request)
                .As<T>();
        }

        public Task<T> GetAsync<T>(string resourceId, string apiVersion)
        {
            var resourceUrl = $"https://management.azure.com/{resourceId.Trim('/')}?api-version={apiVersion}";
            return Client.GetAsync(resourceUrl).As<T>();
        }
    }

    [EntryPoint("Update VMSS Capacity")]
    public class ProgramOptions
    {


        [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        [Option("VmssResourceId", HelpText = "The VMSS Resource Id to Patch capacity on")]
        public string VmssResourceId { get; set; }

        [Option("Capacity", HelpText = "The Capacity")]
        public int Capacity { get; set; }

    }
    class Program
    {

        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
#if DEBUG

            args = args.LoadFrom<ProgramOptions>(@"c:\dev\credsSinno.txt")
               .Concat(new[] {
                "--Capacity","5",
                "--VmssResourceId","/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/ci-sf-tests/providers/Microsoft.Compute/virtualMachineScaleSets/nt1vm",

           }).ToArray();

            args = new[] { "--build" };
#endif

            try
            {
                RunAsync(args, cancellationTokenSource.Token).Wait();
            }
            finally
            {
                runCompleteEvent.Set();
            }
        }

        private static async Task RunAsync(string[] args, CancellationToken token)
        {
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Parsing arguments", args);
            var client = new ArmClient(options.ConnectedServiceName.GetToken("https://management.azure.com/"));

            var resource = await client.GetAsync<JObject>(options.VmssResourceId, "2016-03-30");

            var obj = await client.PatchAsync(options.VmssResourceId, new JObject(
                   new JProperty("sku", new JObject(
                       new JProperty("capacity", options.Capacity),
                       new JProperty("name", resource.SelectToken("$.sku.name").ToString()),
                       new JProperty("tier", resource.SelectToken("$.sku.tier").ToString())
                       ))
                   ), "2016-03-30");

            Console.WriteLine(obj.ToString());
        }
    }
}
