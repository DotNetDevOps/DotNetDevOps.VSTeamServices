using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

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

        [Option("RemoveLeadingZeros")]
        public bool RemoveLeadingZeros { get; set; }

        [Option("FixHyphen")]
        public bool FixHyphen { get; set; }

    }
    public class Program
    {

        static void Main(string[] args)
        {
#if DEBUG
          //  args = new[] { "--build" };
#endif

            var ops = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Runing SemVer Manipuaton", args);

            if (ops.StripPrereleases && ops.SemVer.IndexOf("-") != -1)
            {
                ops.SemVer = ops.SemVer.Substring(0, ops.SemVer.IndexOf("-"));
            }

            if (ops.RemoveLeadingZeros)
            {
                var parts = ops.SemVer.Split('-');
                if (parts.Length > 1)
                {
                    ops.SemVer = parts[0] + "-" + string.Join("-", parts[1].Split('.').Select(StripLeadingZeros));
                }
            }

            if (ops.FixHyphen)
            {
                var parts = ops.SemVer.Split('-');
                if (parts.Length > 1)
                {
                    ops.SemVer = parts[0] + "-" + string.Join(".", parts.Skip(1));
                } 
            }

            TaskHelper.SetVariable(ops.VariableName, ops.SemVer);

        }

        private static string StripLeadingZeros(string arg)
        {
            return arg.TrimStart('0');
        }
    }
}
