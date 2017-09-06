using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.KeyVault.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;
using Microsoft.Azure.KeyVault;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyInformationalVersion("1.0.7")]
[assembly: AssemblyTitle("Install Certificate")]
[assembly: AssemblyDescription("Install a certificate to the current user store")]
[assembly: AssemblyCompany("S-Innovations v/Poul K. Sørensen")]
[assembly: AssemblyProduct("DownloadAndInstallCertificateTask")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyConfiguration("Utility")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("65b3c853-cb34-405e-ad2e-b3d42b765d2d")]

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
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace DownloadAndInstallCertificateTask
{
    [EntryPoint("Adding $(CertificateName) to $(WebAppName)")]
    [Group(DisplayName = "Variable Outputs", isExpanded = true, Name = "outputvariables")]
    public class ProgramOptions
    {


       // [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        public class ConnectedServiceRelation : PropertyRelation<ProgramOptions, ServiceEndpoint>
        {
            public ConnectedServiceRelation()
                : base(options => options.ConnectedServiceName)
            {

            }
        }

        [ConnectedServiceRelation(typeof(ConnectedServiceRelation))]
        [Display(ResourceType = typeof(KeyVaultOutput<ProgramOptions>))]
        public KeyVaultOutput<ProgramOptions> KeyVault { get; set; }

        [Display(GroupName = "outputvariables")]
        [Option("ThumbprintVariableName", HelpText = "The variablename to output thumbprint into")]
        public string ThumbprintVariableName { get; set; }

       
        [Option("SecretValue", HelpText = "The secret value to unpack")]
        public string SecretValue { get; set; }

    }
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = args.LoadFrom<ProgramOptions>(@"c:\dev\credsEarthml.txt")
                .Concat(new[] {
                    "--KeyVaultName", "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/ci-sf-tests/providers/Microsoft.KeyVault/vaults/cikvtest-4w6tzgqkautoc4",
                    "--SecretName", "citestcert",
                    "--ThumbprintVariableName", "citestcert-thumbprint",
                   // "--SecretValue", "="
                }).ToArray();
#endif
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Parsing arguments", args);

            if (!string.IsNullOrEmpty(options.SecretValue))
            {
                X509Certificate2 cert = InstallCertificate(options.SecretValue);
            }
            else  if (options.KeyVault.IsConfigured())
            {

                var vaultUri = $"https://{options.KeyVault.VaultName}.vault.azure.net";
                Console.WriteLine($"Attempting to retrieve secret for {vaultUri}/secrets/{options.KeyVault.SecretName}");

                var secret = options.KeyVault.KeyVaultClient.GetSecretAsync($"{vaultUri}/secrets/{options.KeyVault.SecretName}").GetAwaiter().GetResult();

                X509Certificate2 cert = InstallCertificate(secret.Value);

                if (!string.IsNullOrEmpty(options.ThumbprintVariableName))
                {
                    TaskHelper.SetVariable(options.ThumbprintVariableName, cert.Thumbprint);
                }

                //    File.WriteAllBytes("c:\\dev\\servtest.pfx", Convert.FromBase64String(obj["data"].ToString()));
            }


        }

        private static X509Certificate2 InstallCertificate(string secret)
        {
            //JsonObject with data,password and type info about certificate;
            var obj = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(secret)));
            var cert = new X509Certificate2(Convert.FromBase64String(obj["data"].ToString()), obj["password"].ToString(), X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            Console.WriteLine($"installing {cert.Thumbprint} to CurrentUser.My");




            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();
            }
            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(cert);
                    store.Close();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to add to localmachine");
            }

            return cert;
        }
    }
}
