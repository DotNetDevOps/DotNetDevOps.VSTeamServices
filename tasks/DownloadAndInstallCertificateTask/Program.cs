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
                    "--SecretValue", "ew0KICAiZGF0YSI6ICJNSUlKdWdJQkF6Q0NDWFlHQ1NxR1NJYjNEUUVIQWFDQ0NXY0VnZ2xqTUlJSlh6Q0NCZ2dHQ1NxR1NJYjNEUUVIQWFDQ0Jma0VnZ1gxTUlJRjhUQ0NCZTBHQ3lxR1NJYjNEUUVNQ2dFQ29JSUUvakNDQlBvd0hBWUtLb1pJaHZjTkFRd0JBekFPQkFpRUNFeGF1Zkd4VVFJQ0I5QUVnZ1RZSFVTUk5Kem9pQlk4VWZRVUZ4YjNTUVc4dnpldWQvUWFaU2xpVUl2WW5kSmU3aEx0REJsRHV2dXdxVnoxRkpSbDB2MHJtOUE2QnVNWXoydGhPd0d5N09BTHdZRitSdUFRaHJZbWdtRXo5WjdTbnR4U1NRakNrbk90eFg5aHcybnJOWGpYSjZBK1NEU2h5cEJJamU4R1g0MWRQQ0I2cnREdzY0cEY3L3VrWFFJYTdUVTkxZ0VlOWtVcHJTcFloR2wrdlpLMWRyaU9OV2ZOM2NQYXVURzMvT3phQ3NJbXBkUG9sRm9zNEZvUnpHZkJoVU1EWS83YVN6SG1qWXRWQWJCcTV2WndDeGJVWGljTDBqMjN1KzdRdlozOEpyenAramY0UksyTnVhay9RREE1YUY1cWhmMFBaUWVpUDk1b0ptcWZya1JWVm9RVTlqTForMUFTWTdKK3VJcDI4YkJESGpKaEtGYmErUGlRdWhpUFlEbmxMRFdhTi85MXVySFFpd2pIdXArR0xJQUxIa0VGUFdocHZSRWU5NUsxMjY3SytKbHprWlVncWhmRmpheDJzZEw3aktZT0ZYQVA5ekMycEZIVk1JblNyVmNDQ1JqUVVGVklWYkpkbmpoRU9iRXZXc3ZXZGdoQkZhcUxRaktxNGNRNXZPemtXcU5rL2dwU0hBdys1R0ZjUUpwcmtSbVpJUWNMbUk0ZnVZTUwzUUlVMklzcGIrOGd3UEh1WE9CR3MycjlWbHl0b3dwZzVxaFpBWkR3MmJIRk1sQ1Z5NU5EUWxFclVIWTlTaXNOQ2ZpY3IvZE5QN2xQWjFCU2NzakFtbmRJc1ZTVVlKRVIvSk93dHFaZFNnNHJ5a0d3ZkJrN3JFUnVTSEY1RTdGUmdpazlRYU5XeStWbVJ4ZDh3ZXJlYk85M1piL21vaThmRUVkYkI3Z3ZNQWtES1F2a1hCMEN5M0pldFhWNlpHT1J3d0c5WC9ic3RUYjUyY2xnMUhXTlVMajUxNmpNdjB4KzlUYXZsUE5zN3VlUFB3MWZiL3ROc2I4Uk9Hd3RYMmk3cDVBN2l2Z1ZHbmU3ZWdvQjJLNjkyU2VIcDRsQnErRTFVTWVPVEI2ZmIyYVA2NVlxY0JjVTZCa013YkFPbmVLbnBiNUdraVo5T2pDUW1wNWJ5L3ZtaWgzeG8yMmsrM0d0aEVWalFsS0JGdm00cDM3NnZGQTU3alBiVmFmaUkya1BjcnphU0d1UUU3M09Kc0FOSmNVSmV1MzR5MHQ4VlRiVFhBdDN2Vmh1eitJMzdXQU1aUlVDR3lQYVFaMGJFOFFMa2o4ZDRycEVHQkl6Z1E1NXpZQkxwczB1QmFjbTQxTHJCaEkwSmRPbGk3RGl6NDQ3U3huQ202SmFkMHhJNzRJWFp6Mk1Sa21RQjllbmo2aHZqNXErUDl6dm1qMzNjN3BsRXR0MUNYSC9laTlxM1FEQWRtdENLTm4xa0dhR2FubXlGeGZlZFJZbFlwSlNadXNjWUhmOC9tT2NSbFdrNmowWFc5QUFBRW40UVcrTlNJeEh3bVo4MkVQeUNCNE43S2QwS05kZ01NQkhxYXoxZmRvaFJHcW90RGh4RGRNK1lrWi9aS2EvM1ZoMklmQ2RhYWI2a205WUg4aGZIRm82NFIzSmh6UExkbGJIVnQxOS9WZHN1d0QzL1ordVJ5V2dCUFVGY3crR0k1SHJtbWZwM2lOTC9oeGlwUUh5SDRvZkpub2JTdFM1VkF0aE5qM0liSDZzUFg1dTcwVEpNRE1ZSTVKbWFWUFAxMDd4bWpTMXBHcXVPME5iNEQwZmo4cW81NWtxT3lZUnhKNHpldlYxZVJOaUFUK2x4WmFZRW9vTGE4L2pHMHhteVBlaGttTlV6SmxqdkE5ODg2akxuaUhVY0NvVEM5WjNQNDZHRVFxZXVDd2Y4eTRZOGMxVC9JT2hCeXVPU1JyNXdscG9EelAvSmpVOTBqSUx0QXBkamxPZDRPRW1IWDBLc3V4VzhHeXh5RU92bFBXUTJZNWJZUjg2WllEcHl6UmMzWUFwVWVJelQxRE9rajFsMktkTGw5OG1EOUErZklENlRaTzNMSEZRVjRKTXJuQUJCQlBTYTQ4QVprU2l4VnJVMTN5VmJMelp4WDBxU3ptSzVkRXF0TmxMVHlDaVptaHdCTGdqVTZSVkNSdmVaVndFU0s5WW9ha01hREdCMnpBVEJna3Foa2lHOXcwQkNSVXhCZ1FFQVFBQUFEQlhCZ2txaGtpRzl3MEJDUlF4U2g1SUFEWUFNd0EzQURjQVl3QmxBR0lBWVFBdEFHVUFOQUF6QUdNQUxRQTBBR0VBWlFBNUFDMEFZZ0JqQURjQU9RQXRBREFBWmdBMkFEWUFOUUJtQURNQU5RQXdBR1FBTUFBeU1Hc0dDU3NHQVFRQmdqY1JBVEZlSGx3QVRRQnBBR01BY2dCdkFITUFid0JtQUhRQUlBQkZBRzRBYUFCaEFHNEFZd0JsQUdRQUlBQkRBSElBZVFCd0FIUUFid0JuQUhJQVlRQndBR2dBYVFCakFDQUFVQUJ5QUc4QWRnQnBBR1FBWlFCeUFDQUFkZ0F4QUM0QU1EQ0NBMDhHQ1NxR1NJYjNEUUVIQnFDQ0EwQXdnZ004QWdFQU1JSUROUVlKS29aSWh2Y05BUWNCTUJ3R0NpcUdTSWIzRFFFTUFRWXdEZ1FJNG5NVThqeU1MdndDQWdmUWdJSURDSmVCVHdoT1ZaSDF3SHZ4N0VlbnBEcFcyUTlBbERnZTNaM01Fb1ZZZjhZYjlMOXRmamhYdXVzV3lLMjF4MDJlTEl4NjkvdTFWREhiYVdiTWluc2FzeC9wbnNyTkZoY3ZodGFpZUd3ZHliOEYrMzk4K1hRUzJwaWFBWkZyNUFEMU4vY2JoOENsZTNjeDdhN3duWEFkdDB6MHVmMHEzM01LbTd2bVlrbXN0dzU4QS9odWczWU1mam1BOUFIYmFteUZEVVIyREVCQVErMlkrc2loV21zNTM3SDBrdE5CZ0NEVjZjc2NaVjNVbmd1M2dEME1MdEhzSWJQa3BqbTNLMjlmczFMZ2xsQnJXcEJXZ2NMS3hRaGkwaWlqNUc2UGpGZWdkbFVXV1p4U2pyeE9IaWQrRTBzR0JhdlBhV1ZzZzJ3dWQwc3hqbWxwMlZ2UCtGVjd1NWV2WmMvN0ZkaGFKTUV0OG9qdkVPOS9tOTk3aS9XeHZHKzJoUlNIOEVXOC82WUFOUFhXOXNFQnhiTllpODdGSDc4Q1RUdnBCcDdaSDlzYUlvSzJuTWF4NSt6TTRPUUtIWXhabFZiVjFmSjFyZ1BxeEZ6NFpTTDZ5VUJGWTVWRU5kbXBQREM4d2U2SFBZczFmRjJLM3JMdXBqeEZmNFp1dkRQSEhqaVkrV1JFN0FNVkF0eTZTNEQ0d0pybkdEY09sSC85Ym5qSTFJemlCeWlaOTgrbFJWMUU1WWh2ZUpWYXVIcnBLUGg5QkZxYU1leVhNR2d4ckcwbWs1OWhTbFh5bStJN3VSekZTZEJtUzZkNU51dURWRTJSM1FTMjhyT0c2UjdmZzZrN2RGN3RMcjdSM2MrcFlkVmIyNEZ0UlZpK0Exc0NpU091ZkcyWWhRS1JSWEtvRVQ4Mm10RzYxSFNDd2RqWE5IaUd1c3prVHZtTEM1OXhvNWVVd1FKVlhyUkRDSFdkK1hBUDdlaEp5cXQ0MHVSdFEzM0JLcDh2eVJjbE1Vc1JFNWM5RXc1YUZxTEIzVjVsZHFPVmViSHBqSzk0QllWSHVNbFp0UjBIN1VaYmZpWHVQbDZiUFcxM1gzUDJtNnkxT28ralJpRERHY3FEdHlxb00vckhKVmFEbVlqeE45dUFxUktvVisvTHB5dDlCcHIzN2FZS05EQnJmK0MzZGEzNCtuQXU5WlRrTnh0VmZudFFjRlMyd2xoT2hjT0REaGJISlJjUHpxNGpFKytXdkVYMEVnWityVVRKLzlqSkFndEJkR2ZnWG9sNUIrZmlQVlZDYXpxcThMMGtGWW1Ga1BaVDJpc3VRVnFpVnV3eWJXcEtuRWFtM2NCZTdmcjl3N1BzRG11UEZJV09NNzUxVDZXdjZ2RjYzaGNKTURzd0h6QUhCZ1VyRGdNQ0dnUVVHWVZIa1NSRFRmb3Q2aGtmUGl3aTBuTmcrNTRFRkowY0h2ZnFDSmJqZDF3akltTm0weCtBcHNodUFnSUgwQT09IiwNCiAgImRhdGFUeXBlIjogInBmeCIsDQogICJwYXNzd29yZCI6ICJnbUVjY3VNUyFhIg0KfQ=="
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

                  // File.WriteAllBytes("c:\\dev\\servtest.pfx", Convert.FromBase64String(obj["data"].ToString()));
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

            File.WriteAllBytes("c:\\dev\\servtest.pfx", Convert.FromBase64String(obj["data"].ToString()));


            return cert;
        }
    }
}
