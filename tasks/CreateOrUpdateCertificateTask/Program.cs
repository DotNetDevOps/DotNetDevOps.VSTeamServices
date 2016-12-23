using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CommandLine;
using Microsoft.Azure.KeyVault;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.ResourceManager.CryptoHelper;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.KeyVault.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace CreateOrUpdateCertificateTask
{
    [EntryPoint("Generting Certificate with $(CertificateName)")]
    [Group(DisplayName = "Add to KeyVault", Name = "KeyVault", isExpanded = false)]
    [Group(DisplayName = "Add to WorkItem", Name = "WorkItem", isExpanded = false)]
    public class ProgramOptions
    {
        public ProgramOptions()
        {
            //  KeyVault = new KeyVaultOutput<ProgramOptions>(k => k.ConnectedServiceName);
        }

        [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        [Required]
        [Display(Description = "The certificate name", Name = "Certificate Name")]
        [Option("CertificateName", Required = true)]
        public string CertificateName { get; set; }

        public class ConnectedServiceRelation : PropertyRelation<ProgramOptions, ServiceEndpoint>
        {
            public ConnectedServiceRelation()
                : base(@class => @class.ConnectedServiceName)
            {

            }
        }

        [ConnectedServiceRelation(typeof(ConnectedServiceRelation))] //Allows the user to pick from dropdown of existing keyvaults/secrets
        [Display(GroupName = "KeyVault", ResourceType = typeof(KeyVaultOutput<ProgramOptions>))]
        public KeyVaultOutput<ProgramOptions> KeyVault { get; set; }





        [Display(Description = "The timespan for the certificate given as P730D for 2 Years", Name = "Expires In")]
        [Option("ExpiresIn",  Default = "P730D")]
        public string ExpiresIn { get; set; }


        [Option("PfxPassword", HelpText = "The password for the generated pfx file")]
        public string PfxPassword { get; set; }

        [Option("SetSecretContentType", Default = true, HelpText = "when enabled the secret will have its content type set to application/x-pkcs12")]
        public bool SetSecretContentType { get; set; }


        [Display(ResourceType = typeof(GlobPath))]
        [Option("OutputPath", HelpText = "Filesystem output to store the pfx file")]
        public string OutputPath { get; set; }





        [Display(GroupName = "WorkItem", Description = "If selected the certificate will be atted to a workitem attachement for download", Name = "Create WorkItem")]
        [Option("OutputToWorkItem")]
        public bool OutputToWorkItem { get; set; }


        [Required]
        [Display(GroupName = "WorkItem")]
        [VisibleRule("OutputToWorkItem = true")]
        [Option("VSTSDefaultCollectionUrl", HelpText = "The Url for VSTS Account, ect ' https://sinnovations.visualstudio.com/DefaultCollection '")]
        public string VSTSDefaultCollectionUrl { get; set; }

        [Required]
        [Display(GroupName = "WorkItem")]
        [VisibleRule("OutputToWorkItem = true")]
        [Option("VSTSProjectName", HelpText = "The Project Name")]
        public string VSTSProjectName { get; set; }

        [Required]
        [Display(GroupName = "WorkItem")]
        [VisibleRule("OutputToWorkItem = true")]
        [Option("TokenForVSTS", HelpText = "The token that has access to work item read/write")]
        public string TokenForVSTS { get; set; }
    }
    class CertificateGeneratorTask
    {
        static void Main(string[] args)
        {
#if DEBUG


            args = args.LoadFrom<ProgramOptions>(@"c:\dev\credsSinno.txt")
                .Concat(new[] {
                        "--CertificateName", "ServiceFabricCert",
                        "--KeyVaultName", "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/TestServiceFabric5/providers/Microsoft.Web/vaults/msgprc-kv-dev-0316",
                        "--SecretName" ,"ServiceFabricCert",
                        "--ExpiresIn", "P730D" ,
                        "--PfxPassword", "123456" ,
                 //       "--OutputPath",@"C:\dev\testcert.pfx"
            }).ToArray();
#endif
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Generating Certificate", args);


            Console.WriteLine($"Generating Certificate CN={options.CertificateName}");
            var cert = Certificate.CreateSelfSignCertificatePfx($"CN={options.CertificateName}", DateTime.UtcNow, DateTime.UtcNow.Add(XmlConvert.ToTimeSpan(options.ExpiresIn)), options.PfxPassword);

            var x509Certificate = new X509Certificate2(cert, options.PfxPassword, X509KeyStorageFlags.Exportable);
            Console.WriteLine($"Certificate {x509Certificate.Issuer} created with thumbprint {x509Certificate.Thumbprint}");

            var isUpdated = true;
            if (options.KeyVault.IsConfigured())
            {
                var result = options.KeyVault.SaveCertificateAsync(cert, options.PfxPassword, options.KeyVault.Tags, TimeSpan.FromDays(90), options.SetSecretContentType ? "application/x-pkcs12": null).GetAwaiter().GetResult();
                x509Certificate = result.Certificate;
                isUpdated = result.Updated;
                TaskHelper.SetVariable($"{options.CertificateName}-secretUri", result.SecretUriWithVersion);

            }
            TaskHelper.SetVariable($"{options.CertificateName}-thumbprint", x509Certificate.Thumbprint);
            if (!string.IsNullOrWhiteSpace(options.OutputPath) && options.OutputPath.EndsWith(".pfx"))
            {
                File.WriteAllBytes(options.OutputPath, x509Certificate.Export(X509ContentType.Pkcs12, options.PfxPassword));
            }

            if (options.OutputToWorkItem && isUpdated)
            {

                var url = $"{options.VSTSDefaultCollectionUrl}/{options.VSTSProjectName}/_apis/wit/workitems/$Product Backlog Item?api-version=1.0";// "https://sinnovations.visualstudio.com/DefaultCollection/S-Innovations MessageProcessor/_apis/wit/workitems/$Product Backlog Item?api-version=1.0";
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", options.TokenForVSTS))));

                var req = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                req.Content = new StringContent($@"[{{""op"":""add"",""path"":""/fields/System.Title"",""value"":""Certificate {x509Certificate.Subject} created with thumbprint {x509Certificate.Thumbprint}""}}]", Encoding.UTF8, "application/json-patch+json");


                var res = client.SendAsync(req).GetAwaiter().GetResult();
                var workitem = JObject.Parse(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                Console.WriteLine(workitem.ToString(Newtonsoft.Json.Formatting.Indented));

                var data = x509Certificate.Export(X509ContentType.Pkcs12, options.PfxPassword);
                var upload = new HttpRequestMessage(HttpMethod.Post, $"{options.VSTSDefaultCollectionUrl}/_apis/wit/attachments?fileName={x509Certificate.Subject}.pfx&api-version=1.0");
                upload.Content = new StringContent(Convert.ToBase64String(cert), Encoding.UTF8, "application/octet-stream");
                var uploadRes = client.SendAsync(upload).GetAwaiter().GetResult();
                var str = uploadRes.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.WriteLine(str);
                var uploadResponse = JObject.Parse(str);
                Console.WriteLine(uploadResponse.ToString(Newtonsoft.Json.Formatting.Indented));

                var attach = $@"[{{""op"": ""add"",""path"": ""/relations/-"",""value"": {{""rel"": ""AttachedFile"",""url"": ""{uploadResponse.SelectToken("url").ToString()}"",""attributes"": {{""comment"": ""Certificate {x509Certificate.Subject}""}} }} }}]";
                var attachReq = new HttpRequestMessage(new HttpMethod("PATCH"), workitem.SelectToken("_links.self.href").ToString() + "?api-version=1.0");

                attachReq.Content = new StringContent(attach, Encoding.UTF8, "application/json-patch+json");

                var attachRes = client.SendAsync(attachReq).GetAwaiter().GetResult();
                Console.WriteLine(attachRes.Content.ReadAsStringAsync().GetAwaiter().GetResult());


            }


        }
    }
}
