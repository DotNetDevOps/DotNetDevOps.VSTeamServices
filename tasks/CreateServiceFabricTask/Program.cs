﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Microsoft.Rest.Azure;
using SInnovations.Azure.ResourceManager.TemplateActions;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;

namespace CreateServiceFabricTask
{

    [EntryPoint("Deploying Service Fabric")]
    public class ServiceFabricOptions : ArmTemplateOptions<ServiceFabricOptions>
    {
        public ServiceFabricOptions() : base("CreateServiceFabricTask.cluster.json", typeof(ServiceFabricOptions).Assembly)
        {

        }
        
    }


    class Program
    {
        static void Main(string[] args)
        {

#if DEBUG

            args = args.LoadFrom<ServiceFabricOptions>(@"c:\dev\credsSinno.txt")
                .LoadFrom<ResourceGroupOptions>(null,
                    o => o.ResourceGroup ?? "TestServiceFabric20",
                    o => o.CreateResourceGroup || true,
                    o => o.DeploymentName ?? "fabric",
                    o => o.ResourceGroupLocation ?? "Azeroth")
                .Concat(new[] {
                  "--DefaultCapacity", "5",
                  "--varcapacity", "0",
                  "--clusterLocation","West Europe",
                  "--clusterName", "pksservicefabric12",
                  "--adminPassword","JgT5KFJK",
                  "--certificateThumbprint","584C645A30253DDA98EF8B7ED09B87F61468F3EE",
                  "--sourceVaultValue", "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/TestServiceFabric12/providers/Microsoft.KeyVault/vaults/kv-3wodhzoece5io3wodhzo",
                  "--certificateUrlValue","https://kv-3wodhzoece5io3wodhzo.vault.azure.net/secrets/ServiceFabricCert/8b95f09984424a4097c0010a1e096b86",
                  "--vmNodeTypeSize","Standard_A0"
            }).ToArray();
            args = new[] { "--build" };
#endif
            try
            {
                var options = ConsoleHelper.ParseAndHandleArguments<ServiceFabricOptions>("Create or updating servicefabric", args);

            }
            catch (CloudException ex)
            {
                Console.WriteLine(ex.Body.Message);
                WriteDetails(ex.Body.Details);
                throw;
            }
        }

        private static void WriteDetails(IList<CloudError> err)
        {
            foreach (var m in err)
            {
                Console.WriteLine(m.Message);
                WriteDetails(m.Details);
            }
        }
    }
}

