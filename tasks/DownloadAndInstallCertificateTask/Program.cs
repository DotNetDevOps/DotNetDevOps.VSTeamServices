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
                    //"--KeyVaultName", "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/ci-sf-tests/providers/Microsoft.KeyVault/vaults/cikvtest-4w6tzgqkautoc4",
                    //"--SecretName", "citestcert",
                    //"--ThumbprintVariableName", "citestcert-thumbprint",
                    "--SecretValue", "ew0KICAiZGF0YSI6ICJNSUlKdWdJQkF6Q0NDWFlHQ1NxR1NJYjNEUUVIQWFDQ0NXY0VnZ2xqTUlJSlh6Q0NCZ0FHQ1NxR1NJYjNEUUVIQWFDQ0JmRUVnZ1h0TUlJRjZUQ0NCZVVHQ3lxR1NJYjNEUUVNQ2dFQ29JSUU5akNDQlBJd0hBWUtLb1pJaHZjTkFRd0JBekFPQkFoRHNVQjZvWWhzMWdJQ0I5QUVnZ1RRL1ZPbVREb1hIL1c3UWkydkUva1NlVXdlQzFOWG5XOFlhYkRrZHJOdWN6cFhiWDNpcllvUE01QUFGcldTL1FRVWQ2UkNlTnNkTzZDZ0I1UmJyTkw2VCtweHNRZXJac3hyVU1wMXNhL0tLVkZsK3hkTFBjNjZ6TVhCU2RmaU9YRnJ6Yi9NYmRWK3BGZEFPVWVVZFdPQVZDYjVHYTFQb1JYNzlSeGlWaEdkQVFaVzVYeUNaVld6a2RXVFM5RVdyZkExWFhiU2tSWGVuQitpdGVwKytHWkVXK2JqSDJIbjh3SUFjeXRZZ2Zsb1RNVUk2WjZ6bldJWDd3cFhyTEtIWXhuMkZtWGNKUGVMdUZ3WkYydUhjaWVzSTF2UkdiN1NDWjFjZ25VZkV0M1NGMDR2a0JjdVVsVmpRTGhVWWtnQVVzaWU5WEZuZGFLYmZHdXdQdjd5bExwS20xanlFd3NIYTVxd2RRNHJBWEFLTTJJcnl3NzlEVlhmeCtaVGFPaGR4QTZRYTRpanJLN1JjY3pKYjRZMlFZNllUV3FnWW1zVWN5QW16RmMyK1JvK0lqejBMVGtZMUgrYklDb1JBaFdubUI3VGk4dS9MUUVuWXpXZzN1ZklzRUYxYmhsZjhQRlo3R1NaWXFYRGg1T0owUk4rQnpVeGtjL2NUdmlXWlJFQ0NZWHFhbjkwTzZYTUVqcVNEYjF6azVyUThUWjVrQWlBMVdKTW9KbGdEc3ZLd1N3Y3Z2dUQ1dHU1czVBVjkvZmlnNFhmYm5tQnhkVmU5bVF0WEE1dmxJNERWRHVPUGVwc0lWM01qQVEyQVZUSm5RSWhGbEZCbkx6NmhENmpKWUFMMmcwLzZFSUdtNjhPM0tRUjhrYmpyTjFLam5BdzBOallKc2RNcGpSS21Yc0NSVllHdEt3Q3Z6L1p1TG1MakRMbE53eXZwWmdJWDZkdktDYzdsTzZjSGFEcEhXWFZadkFRQnFlRlRKTXFqMFArV25ZcGdiWUlrdWd1MzkxVWFMRWFjNlBGZUZBQUZJR0p0M0NKZ1pENEV4c25vYXZMNGNUZlVoUmk4NTI1LzRtckdSTWU2eVppSTNQa01SMDV6TDZRR0Jtd21saXNkbS9LOUdiVW15TVRScHJWY1hUVWJPZEdNV0t2YU1hakJVazBGN3FsK2pydUJaV010U01yaSt6bHdKcFlhaVBiMTNBT0VYMmpMR205b1JWZ3lObTV6NXBNTEFxbWc0ZHdMemNzcFNwOW1jVnRXc2s1VUFWbk1tckhza3hoQmdVemFlbnZKdG43dlEwN0NaQTJlZGY0ZkdOME9Vc3MrbFNnbzUzM3U0ZmoxVEZOQUE5YkFzSU9wTTNDeEVMejZ6TzU0WW9WY2FvOENPdVJxd013MWRjd0xJaU42SHdmaS9DRXZPS1Z4cGg0Vi9RdU9LTUsvYXlmWGZXbGQzTEVkVlBqT3ZLMUhraWFBb1RLNHVHN3A2OHBVTU5OQzQ0ak54TTBlaGtDakF5Vkkzb3ZETVg4MXlRVGt1WERzdU1JNDJBdzYrdXRkOGUyRE9HejdnV2w4Uk1zQmd2ck93WFowL3h1ZGF4blRCMGtYVGR5eU56bXgvUU9QT3FCM05HL1FQUXBSc25mclFoeEJqZ3Baakx1WVV1ZXFxSlF2UkRGa1M1SklpbytsQUtPdjhmWjBCVTlTbVBKblY3TDRvQ3cwSVdpV1NvMFZaem9tWlhOeGVFN001Z3RkYysxSVZ5M2Z4ZDJMYXBnMjlVcDcyTUxobGoxaHllOUV2bGdqRWtzaWJPRjFzSTExSWs3N0JSMVpubUk0TDYydkVHYTVXMHNkTndCR3JteEFmNytBRmM0ZG1Wcmc5YzVEZTRVem5OeXdjRW9oR2RvaG5PT2lQZnc3OE5TSFBVUGVzOXozbVUyMlVEK2J5ajBsaEZtTkRmalZnSlBwQXVLOE9UTTNJUHdDekFhWnFoQWNpeUpQN2tqcHl3OVhKZmRscnUyRzFhN1J5T05ZOHA1ZVVnaC84cVlFN0JRQzFIQ0JReXEydWNiWmpiSmdxMkxZYjBreHRWV29ickk5Nms3dHpwK1hnTkVsbXJjNndkWGIxVzA0REN4SW9wQzNJL0ZGWTBLVjQ0UXlGbUtzWlVWVmlFSDJLQTByVlhJUFZPUDl3aFFMSHZqWDlOSmNpRTFoNm94Z2Rzd0V3WUpLb1pJaHZjTkFRa1ZNUVlFQkFFQUFBQXdWd1lKS29aSWh2Y05BUWtVTVVvZVNBQTBBREVBT1FCakFEVUFOZ0ExQURNQUxRQmxBRGNBWmdBd0FDMEFOQUF3QURnQU5RQXRBR0VBTmdCbUFEY0FMUUF3QURJQVlnQm1BRE1BTlFBeUFERUFZUUF3QUdFQU56QnJCZ2tyQmdFRUFZSTNFUUV4WGg1Y0FFMEFhUUJqQUhJQWJ3QnpBRzhBWmdCMEFDQUFSUUJ1QUdnQVlRQnVBR01BWlFCa0FDQUFRd0J5QUhrQWNBQjBBRzhBWndCeUFHRUFjQUJvQUdrQVl3QWdBRkFBY2dCdkFIWUFhUUJrQUdVQWNnQWdBSFlBTVFBdUFEQXdnZ05YQmdrcWhraUc5dzBCQndhZ2dnTklNSUlEUkFJQkFEQ0NBejBHQ1NxR1NJYjNEUUVIQVRBY0Jnb3Foa2lHOXcwQkRBRUdNQTRFQ1BtQkx6SkRmR1B5QWdJSDBJQ0NBeEFsdU5CcTA2NjdmRHpYNUtkYWFVRnZOQUFKUjRKNkd6ZmRKVkRMSTljelVBS0sxelM3VDZWWEFEMERwK3dSSFN3RE42Wk1NZ0FjS0pWSXkzbm42VlFVSUphOHFKRVNqSVQ1eWFPajRzUzFwWHNoY0ljenFnZzZON2djMmpmQkkwOXhETWVid0dTeE52bXBnY1VjTXh6Mzg2UHZjSXNzTTA1WlJoajdZcWhlNWlndHVYOHhwTGYveFhoaUtLV0I1Z0dyY1FOWXF4WWJyTDRTbUlkenZuY3duRGp0YjFNbUdZRTBhUkJZcnhyazg0bmExcVU3NmtaZFZGWmwvc01BVGpxK2tOMy9KUlc2OVp5STJuS2lzRnZvd0FTTEYxc2UrS29Tdmd6bE0yODkxNk1PbWF2NXpSeVRjZS84eGQycElkcVlIUnhPVnBIY1VGSmVZRWduYnRQQkFoNUVFYkswWWFKVEhEeUhLektaM2gwYWNkYTQ2ZDF4SWhueUNrbzZkZWxtR0tGVlJvSnVFSHFTRERlVnhEdS9mY1UwMHlHaXp0T2V6L251Q0dGUWQ0dnpCRi9sZkRxQ1ZqMWM2RDg5a0tENW1vQmN2V2cxZWQ3YTJzblVKQ1NRWS9FdzdKTXhuaGwxeU1kYW9rbU4zQndITWdpVzJnVWJJdi9kSG5TNVI2SFhWMVo2eHdjWjIwSktIODVtZkVleVdoMGZlU0RMT1psa0JobHl4b01kL1NDSmJ5S1BtTTdseFkzQWhkZHZGZ2hkamVzNWwzT1VCZWtieUgxNCsySEh1WTRWRVA1c2xldVV1bmdlU2NzNEErem5yMnM5alJWeHh0UHpCektqbmpmV1ltc09EUUV2eWNKaVFFT0FINFBTbnRybW5KdjAwU3YwWHRQdzF5NTZhck5zMXRtbnNDRU1tajEweFV6VDk0c0hBTDVxZUkxbTRuYk90Y3JUcUJNaDZiT25kRWZKdFUzUDJsSXRUTWZZdTdjMjVhVDNPRTJoVStxdDV1bFg1Vk1rQk0yeFNkLzB6UU9LcU4zQVZnZ3JyNzJUN21YSnNtcWtGbXFnZnhvZzRoWFRQYm53UTYyL0MxRnd3V2JTcTB4OGhoL0djK3NaZmFsSlpISTFFTHJ0VFVmMkNDQkVLdW80ZjBCTWNxQVA1MzI2UytGRkprbERLY1pPVjQ0ZUlabEVzRGdDa04yK3pVd293QU1sUWhWK01xaEVvYVlwUGtTTzBTNS9lQ0pYMUd1R2ZzVGdxc1h1NTBUeU54aFF3NE8wRWRiN3U4UTZuVDRzTVZWdXI3aVAraWJ1SjVLa1psRkRsMWtNZTJzQmI2azNBZDRob3h5bFNMYTBwdEU3dEs0aGs5NXBlLzI3MnFhRU5oT3FxVTFPTURzd0h6QUhCZ1VyRGdNQ0dnUVVYNUEyWGtXcVpqdXV0d2FKRzVpY0VLRTlpN2NFRk9vUWRmazZMTGt1M0Q4UEx5WGVxdmdHdmd3Z0FnSUgwQUFBQUFBQUFBQUEiLA0KICAiZGF0YVR5cGUiOiAicGZ4IiwNCiAgInBhc3N3b3JkIjogIjEyMzQ1NiINCn0="
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
