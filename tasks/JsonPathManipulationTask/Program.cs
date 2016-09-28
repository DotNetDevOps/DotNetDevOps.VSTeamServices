using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace JsonPathManipulationTask
{
    [EntryPoint("Configuring Json file using JsonPath")]
    public class ProgramOptions
    {
        [Display(ShortName = "JsonFile", Name = "Json File", Description = "Path to the json file to update", ResourceType = typeof(GlobPath))]
        public GlobPath JsonFiles { get; set; }

        [Option("JsonPathFilter", HelpText = "The JsonPath filter to select token with")]
        public string JsonPathFilter { get; set; }

        [Option("ReplacementValue", HelpText = "The value to substritude the selected token with")]
        public string ReplacementValue { get; set; }
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

            foreach (var file in options.JsonFiles.MatchedFiles())
            {
                var json = JToken.Parse(File.ReadAllText(file));

                var token = json.SelectToken(options.JsonPathFilter);
                token.Replace(options.ReplacementValue);
              //  var property = token.Parent as JProperty;
              //  property.Value = options.ReplacementValue;

                File.WriteAllText(file, json.ToString());
            }


        }
    }
}
