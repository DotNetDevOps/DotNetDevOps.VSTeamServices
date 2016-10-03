using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CommandLine;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace AzureWebAppDeploymentTask
{

    [EntryPoint("Deploying $(WebApp)")]
    [Group(DisplayName = "AspNet Core Settings", Name = "AspNetCore", isExpanded = true)]
    [Group(DisplayName = "Pull Request Options", Name = "PullRequest", isExpanded = true)]
    public class ProgramOptions
    {

        [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }


        [Display(Description = "The resource group for the webapp to output certificate to", Name = "WebApp Resource Group")]
        [Option("WebAppResourceGroupName")]
        public string WebAppResourceGroupName { get; set; }

        [Required]
        [Display(Description = "The WebApp", Name = "WebApp Name")]
        [Option("WebApp")]
        public string WebApp { get; set; }

        [Display(Description = "The DeployIisAppPath to deploy to", Name = "DeployIisAppPath")]
        [Option("DeployIisAppPath")]
        public string DeployIisAppPath { get; set; }


        [Display(Description = "The WebApp package", Name = "WebApp Package", ShortName = "WebAppSource", ResourceType = typeof(GlobPath))]
        [Option("WebAppSource")]
        public string WebAppSource { get; set; }

        //[Display(Description = "The HttpPlatformHandler ProcessPath", Name = "HttpPlatformHandler ProcessPath")]
        //[Option("MoveWebConfigToParent")]
        //public bool MoveWebConfigToParent { get; set; }

        [Display(GroupName = "AspNetCore")]
        [Option("DNXDeployment", HelpText = "DNS Deployment Properties", DefaultValue = false)]
        public bool DNXDeployment { get; set; }

        [VisibleRule("DNXDeployment == true")]
        [Display(GroupName = "AspNetCore", Description = "The HttpPlatformHandler ProcessPath", Name = "HttpPlatformHandler ProcessPath")]
        [Option("ProcessPath")]
        public string ProcessPath { get; set; }


        [Display(GroupName = "PullRequest", Description = "Create Virtual Application for each Pull Request", Name = "Create Virtual Application")]
        [Option("CreateVirtualAppForPullRequest")]
        public bool CreateVirtualAppForPullRequest { get; set; }

        [Display(Description = "Restart Azure App Service", Name = "Restart AppService")]
        [Option("RestartAppService")]
        public bool RestartAppService { get; set; }

        [Display(Description = "AppOfflineRule", Name = "AppOfflineRule")]
        [Option("AppOfflineRule")]
        public bool AppOfflineRule { get; set; }
    }
    public enum AppType
    {
        IisApp,
        package,
        contentPath
    }
    public class MsPublishArgs
    {
        public AppType SourceType { get; set; }
        public AppType DestType { get; set; }
        public string SourcePath { get; set; }
        public string PublishingUserName { get; set; }
        public string PublishingPassword { get; set; }
        public string DeployIisAppPath { get; set; }
        public string MSDeployServiceUrl { get; internal set; }

        public bool AppOfflineRule { get; set; }
        public override string ToString()
        {
            var source = GetSource();
            var dest = GetDest();

            var args = new List<string>()
            {
                source,
                dest,
                "-verb:sync",
              //  "-enableRule:DoNotDeleteRule", (TODO, make option)
              //  "-enableLink:contentLibExtension",        (this was used in dnx, uncomment now)       
                "-retryAttempts=20",
                "-verbose",
                "-userAgent=\"SInnovations:VSTSPublishTask: WTE14.0.51027.0\""
            };

            if (AppOfflineRule)
            {
                args.Add("-enablerule:AppOffline");
            }

            return string.Join(" ", args);
        }

        private string GetSource()
        {
            switch (SourceType)
            {
                case AppType.IisApp:
                case AppType.contentPath:
                case AppType.package:
                    return $"-source:{SourceType}='{SourcePath}'";

            }
            throw new NotImplementedException();

        }

        private string GetDest()
        {
            switch (DestType)
            {
                case AppType.IisApp:
                    return $"-dest:{DestType}='{DeployIisAppPath}',ComputerName='{MSDeployServiceUrl}?site={DeployIisAppPath}',UserName='{PublishingUserName}',Password='{PublishingPassword}',IncludeAcls='False',AuthType='Basic'";
            }
            throw new NotImplementedException();
        }
    }


    class test : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var respon = await base.SendAsync(request, cancellationToken);

            return respon;
        }
    }
    class AzureWebAppDeploymentTask
    {
     
        // private static object publishArgs;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Deployin web app", args);

            if (options.WebApp.StartsWith("/subscriptions"))
                options.WebApp = options.WebApp.Split('/').Last();

            var managemenetToken = options.ConnectedServiceName.GetToken("https://management.azure.com/");

            using (var client = new WebSiteManagementClient(
                   new TokenCredentials(managemenetToken), new test()))
            {
                client.SubscriptionId = options.ConnectedServiceName.SubscriptionId;

                var site = client.Sites.GetSite(options.WebAppResourceGroupName, options.WebApp);
                var cred = client.Sites.ListSitePublishingCredentials(options.WebAppResourceGroupName, options.WebApp);

                //this will mask the credentials in logs.
                TaskHelper.SetVariable("si_webapp_deployment_cred", cred.PublishingPassword, true);


                //TODO, msdeploy can handle this.
                var folder = "tmpdeploy";
                if (options.WebAppSource.EndsWith(".zip"))
                {
                    if (Directory.Exists(folder))
                        Directory.Delete(folder, true);

                    ZipFile.ExtractToDirectory(options.WebAppSource, folder);
                    var xml = XDocument.Load(Path.Combine(folder, "archive.xml"));
                    var path = xml.Root.Element("iisApp").Attribute("path");
                    var local = path.Value.Replace(@"C:", Path.Combine(folder, "Content", "C_C"));
                    Console.WriteLine(local);
                    options.WebAppSource = Path.GetFullPath(local);
                }
                //For DNX
                if (!string.IsNullOrEmpty(options.ProcessPath))
                {
                    var config = Path.Combine(options.WebAppSource, "web.config");
                    var xml = new XmlDocument();
                    xml.Load(config);
                    xml.SelectSingleNode("//httpPlatform/@processPath").InnerText = options.ProcessPath;
                    xml.SelectSingleNode("//httpPlatform/@stdoutLogFile").InnerText = @"%home%\LogFiles\stdout.log";
                    xml.Save(config);
                }

                if (options.CreateVirtualAppForPullRequest)
                {

                    var buildInfo = JObject.Load(new JsonTextReader(new StreamReader((Path.Combine(options.WebAppSource, "buildinfo.json")))));
                    var repositoryName = buildInfo.SelectToken("pullRequest.repository.name")?.ToString().ToLower().Replace(".", "-");
                    var sourceRefName = buildInfo.SelectToken("pullRequest.sourceRefName")?.ToString();
                    var pullRequestId = buildInfo.SelectToken("pullRequest.pullRequestId")?.ToString();

                    if (!string.IsNullOrEmpty(pullRequestId))
                    {
                        var appConfig = client.Sites.GetSiteConfig(options.WebAppResourceGroupName, options.WebApp);

                        EnsureVirtualAppCreated(appConfig, "/pr", $@"site\pr");
                        EnsureVirtualAppCreated(appConfig, $"/pr/{repositoryName}", $@"site\pr\{repositoryName}");
                        EnsureVirtualAppCreated(appConfig, $"/pr/{repositoryName}/{pullRequestId}", $@"site\pr\{repositoryName}\{pullRequestId}");

                        client.Sites.CreateOrUpdateSiteConfig(options.WebAppResourceGroupName, options.WebApp, appConfig);

                        options.DeployIisAppPath = $"/pr/{repositoryName}/{pullRequestId}";

                    }

                }

                if (!string.IsNullOrEmpty(options.DeployIisAppPath) && options.DNXDeployment)
                {
                    var config = Path.Combine(options.WebAppSource, "web.config");
                    Console.WriteLine(config);

                    var xml = new XmlDocument();
                    xml.Load(config);


                    var http = xml.SelectSingleNode("//httpPlatform");
                    var env = xml.SelectSingleNode("//environmentVariables");
                    if (env == null)
                    {
                        env = CreateOrSelectElement(http, "environmentVariables");
                    }
                    Console.WriteLine(env);

                    CreateEnvironmentVariable(env, "VIRTUAL_PATH", $"/{options.DeployIisAppPath.Trim('/')}");


                    xml.Save(config);
                }

                var publishArgs = new MsPublishArgs()
                {
                    SourceType = AppType.IisApp,
                    DestType = AppType.IisApp,
                    AppOfflineRule = options.AppOfflineRule,
                    SourcePath = options.WebAppSource,
                    PublishingUserName = cred.PublishingUserName,
                    PublishingPassword = cred.PublishingPassword,
                    MSDeployServiceUrl = $"https://{options.WebApp}.scm.azurewebsites.net/msdeploy.axd",
                    DeployIisAppPath = GetDeployIisAppPath(options)
                };



                try
                {
                    if (options.RestartAppService)
                    {
                        client.Sites.StopSite(options.WebAppResourceGroupName, options.WebApp);
                    }
                    Console.WriteLine($"{GetMSDeploy()} {publishArgs}");
                    var tokenS = new CancellationTokenSource(2 * 60 * 1000);

                    Task a = DeployAsync(publishArgs, tokenS.Token);

                    a.Wait();

                    Environment.ExitCode = a.IsFaulted || a.IsCanceled ? -1 : 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Environment.ExitCode = 10;
                    throw;

                }
                finally
                {
                    if (options.RestartAppService)
                    {

                        client.Sites.StartSite(options.WebAppResourceGroupName, options.WebApp);
                    }
                }
                //catch any leftovers in redirected stdout

            }
        }

        private static Task DeployAsync(MsPublishArgs publishArgs, CancellationToken token)
        {
            return Task.Factory.StartNew(() =>
            {
                var p = new ProcessStartInfo(GetMSDeploy(), publishArgs.ToString())
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };

                var cmd = Process.Start(p);
                do
                {
                    Thread.Sleep(100);
                    Console.Out.Write(cmd.StandardOutput.ReadToEnd());
                    if (token.IsCancellationRequested)
                    {
                        cmd.Kill();
                        token.ThrowIfCancellationRequested();
                    }
                }
                while (!cmd.HasExited);

                Console.Out.Write(cmd.StandardOutput.ReadToEnd());
            }, TaskCreationOptions.LongRunning);
        }

        private static void CreateEnvironmentVariable(XmlNode env, string key, string value)
        {
            var variable = CreateOrSelectElement(env, "environmentVariable", $"[@name='{key}']");

            var name = variable.Attributes["name"] ?? variable.Attributes.Append(variable.OwnerDocument.CreateAttribute("name"));
            name.Value = key;
            var valueAtt = variable.Attributes["value"] ?? variable.Attributes.Append(variable.OwnerDocument.CreateAttribute("value"));
            valueAtt.Value = value;
        }

        private static XmlNode CreateOrSelectElement(XmlNode env, string ename, string filter = "")
        {

            return env.SelectSingleNode($".//{ename}{filter}") ?? env.AppendChild(env.OwnerDocument.CreateElement(ename, env.NamespaceURI));

        }

        private static void EnsureVirtualAppCreated(SiteConfig appConfig, string virt, string phy)
        {
            if (!appConfig.VirtualApplications.Any(p => p.VirtualPath == virt))
            {
                appConfig.VirtualApplications.Add(new VirtualApplication
                {
                    PhysicalPath = phy,
                    VirtualPath = virt
                });
            }
        }

        private static string GetDeployIisAppPath(ProgramOptions options)
        {
            if (string.IsNullOrEmpty(options.DeployIisAppPath))
                return options.WebApp;
            return $"{options.WebApp}/{options.DeployIisAppPath.Trim('/')}";
        }


        public static string GetMSDeploy()
        {
            return Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), @"IIS\Microsoft Web Deploy V3\msdeploy.exe");
        }
    }
}
