using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace JsonPathExtractToVariableTask
{
    [EntryPoint("Extracting $(JsonPathFilter) to $(VariableName)")]
    public class ProgramOptions
    {
        [Display(ShortName = "JsonFile", Name = "Json File", Description = "Path to the json file to update", ResourceType = typeof(GlobPath))]
        public string JsonFile { get; set; }

        [Option("JsonPathFilter", HelpText = "The JsonPath filter to select token with")]
        public string JsonPathFilter { get; set; }

        [Option("VariableName", HelpText = "The variable to set with the extracted inforation")]
        public string VariableName { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            //args = new[]
            //{    
            //        "--JsonFile", @"bin\Debug\net46\win7-x64\task.json",
            //        "--JsonPathFilter", "$.version",
            //        "--ReplacementValue", "1.0.2"
            //};
#endif
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Editing Json File", args);

            var json = JToken.Parse(File.ReadAllText(options.JsonFile));

            var token = json.SelectToken(options.JsonPathFilter);


            TaskHelper.SetVariable(options.VariableName, token.ToString());


        }
    }
}
