﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace VSTSBuildInfoTask
{
    [EntryPoint("Creating Build Artifact")]
    public class ProgramOptions
    {
        [Display(Description = "The path to save the artifact file", Name = "Output File Name")]
        [Option("OutPutFileName")]
        public string OutPutFileName { get; set; }

        /// <summary>
        /// When used a varaible can be set with a feature name to be used in output/distribution names
        /// </summary>

        [Display(Description = "The regex pattern to select a feature name with", Name = "Feature Regex Pattern")]
        [Option("FeatureRegexPattern")]
        public string FeatureRegexPattern { get; set; }

        [Display(Description = "The feature name output variable", Name = "Feature Variable Name")]
        [Option("FeatureVariableName")]
        public string FeatureVariableName { get; set; }

    }

    class Program
    {
        static void Main(string[] args)
        {
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Creating Build Artifact", args);

            var teamUri = Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI");
            var teamProject = Environment.GetEnvironmentVariable("SYSTEM_TEAMPROJECT");
            var featureName = "ci";


            var token = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");
            Console.WriteLine(token);
            //refs/pull/32/merge
            var buildSourceBranche = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");

            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            var obj = JObject.FromObject(new
            {
                Build = new
                {
                    DefinitionName = Environment.GetEnvironmentVariable("BUILD_DEFINITIONNAME"),
                    DefinitionVersion = Environment.GetEnvironmentVariable("BUILD_DEFINITIONVERSION"),
                    BuildNumber = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER"),
                    BuildUri = Environment.GetEnvironmentVariable("BUILD_BUILDURI"),
                    BuildId = Environment.GetEnvironmentVariable("BUILD_BUILDID"),
                    QueuedBy = Environment.GetEnvironmentVariable("BUILD_QUEUEDBY"),
                    QueuedById = Environment.GetEnvironmentVariable("BUILD_QUEUEDBYID"),
                    RequestedFor = Environment.GetEnvironmentVariable("BUILD_REQUESTEDFOR"),
                    BuildRequestedForId = Environment.GetEnvironmentVariable("BUILD_REQUESTEDFORID"),
                    BuildSourceVersion = Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION"),
                    BuildSourceBranch = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH"),
                    BuildSourceBranchName = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME"),
                    Repository = new
                    {
                        Name = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_NAME"),
                        Provider = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_PROVIDER"),
                        Uri = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_URI"),
                    }
                }
            }, serializer);


            if (buildSourceBranche.StartsWith("refs/pull"))
            {
                var pullRquestId = Regex.Match(buildSourceBranche, "refs/pull/(.*)/merge").Groups[1].Value;
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var repositoryId = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_ID");
                var pr = httpClient.GetStringAsync($"{teamUri}_apis/git/repositories/{repositoryId}/pullRequests/{pullRquestId}?api-version=3.0").GetAwaiter().GetResult();

                obj["pullRequest"] = JObject.Parse(pr);

                var sourceRefName = obj.SelectToken("$.pullRequest.sourceRefName")?.ToString();
                if (!string.IsNullOrEmpty(options.FeatureRegexPattern) && !string.IsNullOrEmpty(sourceRefName))
                {
                    featureName = Regex.Match(sourceRefName, options.FeatureRegexPattern,RegexOptions.RightToLeft).Captures[0].Value;
                }

            }

            if (!string.IsNullOrEmpty(options.FeatureVariableName))
            {
                TaskHelper.SetVariable(options.FeatureVariableName, featureName);
            }




            Console.WriteLine(obj
               .ToString(Newtonsoft.Json.Formatting.Indented));

            File.WriteAllText(options.OutPutFileName, obj
               .ToString(Newtonsoft.Json.Formatting.Indented));

        }
    }
}
