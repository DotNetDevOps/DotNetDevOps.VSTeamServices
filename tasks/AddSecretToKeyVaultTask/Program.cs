using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.KeyVault.ResourceTypes;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyInformationalVersion("1.0.7")]
[assembly: AssemblyTitle("Add Secret to KeyVault")]
[assembly: AssemblyDescription("Add a secret to a Azure KeyVault")]
[assembly: AssemblyCompany("S-Innovations v/Poul K. Sørensen")]
[assembly: AssemblyProduct("AddSecretToKeyVaultTask")]
[assembly: AssemblyConfiguration("Utility")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("bcdf9a5e-1309-4b8a-b9fd-49845b276d62")]

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


namespace AddSecretToKeyVaultTask
{

    [EntryPoint("Adding secret to keyvault")]
    public class ProgramOptions
    {
        [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        public class ConnectedServiceRelation : PropertyRelation<ProgramOptions, ServiceEndpoint>
        {
            public ConnectedServiceRelation()
                : base(@class => @class.ConnectedServiceName)
            {

            }
        }

        [ConnectedServiceRelation(typeof(ConnectedServiceRelation))]
        [Display(ResourceType = typeof(KeyVaultOutput<ProgramOptions>))]
        public KeyVaultOutput<ProgramOptions> KeyVault { get; set; }

        [Required]
        [Display(Description = "The value to set")]
        [Option("Value")]
        public string Value { get; set; }

    }
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = args.LoadFrom<ProgramOptions>(@"c:\dev\credsEarthml.txt")
                .Concat(new[] {
                    "--KeyVaultName", "",
                    "--SecretName", "test",
                    "--Value", "dasda"
                    //"--ThumbprintVariableName", "citestcert-thumbprint",
                }).ToArray();

            args = new[] { "--build" };
#endif

            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Parsing arguments", args);

            if (options.KeyVault.IsConfigured())
            {
                options.KeyVault.SetSecretIfNotExistAsync(options.Value).Wait();
            }

        }
    }
}
