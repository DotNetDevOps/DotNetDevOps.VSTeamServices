using System;
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

        public override void OnTemplateLoaded()
        {

            this.ArmDeployment.AfterLoad.Add((template) =>
            {
                var tenantId = template.SelectToken("$.variables.tenantId").ToString();
                var clientApplication = template.SelectToken("$.variables.clientApplication").ToString();
                var clusterApplication = template.SelectToken("$.variables.clusterApplication").ToString();
                if(string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientApplication) || string.IsNullOrEmpty(clusterApplication))
                {
                    var jprop = template.SelectToken("$.resources[?(@.type=='Microsoft.ServiceFabric/clusters')].properties.azureActiveDirectory");

                    if (jprop != null)  //Remove the JProperty on parent
                        jprop.Parent.Remove();
                 
                }
            });

            base.OnTemplateLoaded();
        }
    }


    class Program
    {
        static void Main(string[] args)
        {

#if DEBUG

            //args = args.LoadFrom<ServiceFabricOptions>(@"c:\dev\credsEarthml.txt")
            //    .LoadFrom<ResourceGroupOptions>(null,
            //        o => o.ResourceGroup ?? "earthml-core",
            //        o => o.CreateResourceGroup || true,
            //        o => o.DeploymentName ?? "earthml-core-sf",
            //        o => o.ResourceGroupLocation ?? "westeurope")
            //    .Concat(new[] {
            //        "--AppendTimeStamp",
            //        "--clusterLocation", "West Europe",
            //        "--clusterName", "earthml",
            //        "--nt0fabricTcpGatewayPort","19000",
            //        "--nt0fabricHttpGatewayPort","19080",
            //        "--adminUserName","pksorensen",
            //        "--adminPassword","safsadsagd22!",
            //        "--nt0InstanceCount","3",
            //        "--loadBalancedAppPort1","80",
            //        "--loadBalancedAppPort2","8081",
            //        "--overProvision","false",
            //        "--certificateStoreValue","My",
            //        "--vmNodeTypeSize","Standard_D1_v2",
            //        "--certificateThumbprint","BCAC5628B020BA5DD0EF6274A86488EAADA0D03D",
            //        "--sourceVaultValue","/subscriptions/1626d2da-4051-4674-9d4c-57ce23d967a3/resourceGroups/earthml-core/providers/Microsoft.KeyVault/vaults/earthml-core-k3ci",
            //        "--certificateUrlValue","https://earthml-core-k3ci.vault.azure.net:443/secrets/sfcert/054bd582c503435aaa6bc5642b259d21",
            //        "--clusterProtectionLevel","EncryptAndSign",
            //        "--storageAccountType","Standard_LRS",
            //        "--supportLogStorageAccountType","Standard_LRS",
            //        "--applicationDiagnosticsStorageAccountType","Standard_LRS",
            //        "--varcomputeLocation","[parameters('clusterLocation')]",
            //        "--vardnsName","[parameters('clusterName')]",
            //        "--varvmStorageAccountName","[toLower(concat(uniqueString(resourceGroup().id), '1' ))]",
            //        "--varvmName","core",
            //        "--varnt0applicationStartPort","20000",
            //        "--varnt0applicationEndPort","30000",
            //        "--varnt0ephemeralStartPort","49152",
            //        "--varnt0ephemeralEndPort","65534",
            //        "--varpublicIPAddressName","[concat(parameters('clusterName'),'-PubIP')]",
            //        "--varpublicIPAddressType","Dynamic",
            //        "--varvmStorageAccountContainerName","vhds",
            //        "--varvirtualNetworkName","VNet",
            //        "--varaddressPrefix","10.0.0.0/16 ",
            //        "--varsubnet0Name","Subnet-1",
            //        "--varsubnet0Prefix","10.0.0.0/24",
            //        "--varnicName","[concat('NIC-',parameters('clusterName'))]",
            //        "--varlbName","[concat('LB-',parameters('clusterName'))]",
            //        "--varlbIPName","[concat('LBIP-',parameters('clusterName'))]",
            //        "--varavailSetName","AvailabilitySet",
            //        "--varmaxPercentUpgradeDomainDeltaUnhealthyNodes","100",
            //        "--varvmImagePublisher","MicrosoftWindowsServer",
            //        "--varvmImageOffer","WindowsServer",
            //        "--varvmImageSku","2012-R2-Datacenter",
            //        "--varvmImageVersion","latest",
            //        "--varvnetID","[resourceId('Microsoft.Network/virtualNetworks',variables('virtualNetworkName'))]",
            //        "--varsubnet0Ref","[concat(variables('vnetID'),'/subnets/',variables('subnet0Name'))]",
            //        "--varsupportLogStorageAccountName","[toLower( concat( uniqueString(resourceGroup().id),'2'))]",
            //        "--varapplicationDiagnosticsStorageAccountName","[toLower(concat(uniqueString(resourceGroup().id), '3' ))]",
            //        "--varlbID0","[resourceId('Microsoft.Network/loadBalancers',concat('LB','-', parameters('clusterName'),'-',variables('vmNodeType0Name')))]",
            //        "--varlbIPConfig0","[concat(variables('lbID0'),'/frontendIPConfigurations/LoadBalancerIPConfig')]",
            //        "--varlbPoolID0","[concat(variables('lbID0'),'/backendAddressPools/LoadBalancerBEAddressPool')]",
            //        "--varlbProbeID0","[concat(variables('lbID0'),'/probes/FabricGatewayProbe')]",
            //        "--varlbHttpProbeID0","[concat(variables('lbID0'),'/probes/FabricHttpGatewayProbe')]",
            //        "--varlbNatPoolID0","[concat(variables('lbID0'),'/inboundNatPools/LoadBalancerBEAddressNatPool')]",
            //        "--varvmNodeType0Name","[toLower(concat('NT1', variables('vmName')))]",
            //        "--varvmNodeType0Size","[parameters('vmNodeTypeSize')]",
            //        "--varvmStorageAccountName0","[toLower(concat(uniqueString(resourceGroup().id), '1', '0' ))]",
            //        "--varuniqueStringArray0",
            //                "[concat(variables('vmStorageAccountName0'), '0')]",
            //                "[concat(variables('vmStorageAccountName0'), '1')]",
            //                "[concat(variables('vmStorageAccountName0'), '2')]",
            //                "[concat(variables('vmStorageAccountName0'), '3')]",
            //                "[concat(variables('vmStorageAccountName0'), '4')]",
            //        "--varvmssApiVersion", "2016-03-30",
            //        "--varlbApiVersion","2015-06-15",
            //        "--varvNetApiVersion","2015-06-15",
            //        "--varstorageApiVersion","2016-01-01",
            //        "--varpublicIPApiVersion","2015-06-15",
            //        "--outclusterProperties","clusterProperties",
            //        "--outprimaryVmssResourceId","primaryVmssResourceId",
            //        "--outmanagementEndpoint","managementEndpoint",
            //        "--outgatewayEndpoint", "gatewayEndpoint",
            //        "--outvmStorageAccountName", "vmStorageAccountName"
            //    }).ToArray();
          //  args = new[] { "--build" };
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

