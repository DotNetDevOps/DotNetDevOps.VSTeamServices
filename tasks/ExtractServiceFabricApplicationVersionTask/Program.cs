using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace ExtractServiceFabricApplicationVersionTask
{
    [EntryPoint("Extracting ServiceFabric Version")]
   
    public class ProgramOptions
    {
        [Required]
        [Display(Description = "The Application Manifest", Name = "ApplicationManifest", ShortName = "ApplicationManifest", ResourceType = typeof(GlobPath))]
        public string Manifest { get; set; }
        
        
        [Option("VariableName", HelpText = "The Variable Name")]
        public string VariableName { get; set; }

        [Option("TypeVariableName", HelpText = "The Type Variable Name")]
        public string TypeVariableName { get; set; }


        [Option("UpdateBuild", HelpText = "Update Build Version")]
        public bool UpdateBuild { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new[] { "--build" };
            args = new[] { "--ApplicationManifest", @"C:\dev\sinnovations\MessageProcessor.ServiceFabric\src\MessageProcessor.ServiceFabricHost\pkg\Release\ApplicationManifest.xml", "--VariableName", "AppVersion", "--UpdateBuild" };
#endif

            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Extracting ServiceFabric Version", args);

            var XDoc = XDocument.Load(options.Manifest);
            var version = XDoc.Root.Attribute("ApplicationTypeVersion")?.Value;
            var typeName = XDoc.Root.Attribute("ApplicationTypeName")?.Value;
            Console.WriteLine("Extracted Version: " + version);
            Console.WriteLine("Extracted Type: " + typeName);

            if (!string.IsNullOrEmpty(options.VariableName))
                TaskHelper.SetVariable(options.VariableName, version);

            if (!string.IsNullOrEmpty(options.TypeVariableName))
                TaskHelper.SetVariable(options.TypeVariableName, typeName);

            if (options.UpdateBuild)
            {
                Console.WriteLine($"##vso[build.updatebuildnumber]{version}");
            }
        }
    }
}
