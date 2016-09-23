using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace SemVerUtilityTask
{
    [EntryPoint("Updating variable $(VariableName)")]
    public class ProgramOptions
    {
        [Option("SemVer")]
        public string SemVer { get; set; }
        [Option("StripPrereleases")]
        public bool StripPrereleases { get; set; }

        [Option("VariableName", HelpText = "The variable to set with the manipulated semver")]
        public string VariableName { get; set; }

    }
    public class Program
    {

        static void Main(string[] args)
        {
#if DEBUG
            args = new[] { "--build" };
#endif

            var ops = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Runing SemVer Manipuaton", args);

            if (ops.StripPrereleases)
            {
                ops.SemVer = ops.SemVer.Substring(0, ops.SemVer.IndexOf("-"));
            }

            TaskHelper.SetVariable(ops.VariableName, ops.SemVer);

        }


    }
}
