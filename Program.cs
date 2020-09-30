using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Samples.Common;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace AzureTask
{
    public class Program
    {
        public static void RunSample(IAzure azure, string clientId, string secret)
        {
            string rgName = SdkContext.RandomResourceName("rgaks", 15);
            string aksName = SdkContext.RandomResourceName("akssample", 30);
            Region region = Region.USCentral;
            string rootUserName = "aksuser";
            string sshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQCtmDbSyJsyclLhecOiuh+QE7JQCakHLSSAtwL0uPir4Vw1j8OWqBnl7RbMbBq7Q35zNFPPrLAN7ZDXzR37I6KUUPoWjL8vhHom/pmOObs85dpjHgWYVu1YfFZNHdS4JgRuPYQ+JXkST87/RtUlogJJhFtmkFcXcwKBtEtoOeqGPnoCwFL8o06igSIqljNJXNHSSejctOuMeMjSfkB/TPihShiaZ0q5XaIZIls5gASMzMox9iFG0YF8ikxv/CwjIiUMZRTbqEpiGAAMOrq8CgcdKvNSpGaekKXHarVhsKTpsgWOY2JK8vDqilg+3m+s03Q0hLMBUbRcp0W3MU7sNmDeZWZQHNyTy9DYI+IHNBoBBbfMu8w8c2FIA38WPWBJlR+LMtqDsjMOYMrALPZRFG6rTX1aAewQYgyS3alfhqKTKDJ2CLnnqgGd4B4t3rxPl3RdYRCd91f/+Heho9wDiLNC14Shibf+dQ5D7Sai0vTn3jm0H0EJjdjxgMxYMHcrjz0= generated-by-azure";
            string servicePrincipalClientId = clientId; // replace with a real service principal client id
            string servicePrincipalSecret = secret; // and corresponding secret

            try
            {
                if (String.IsNullOrWhiteSpace(servicePrincipalClientId) || String.IsNullOrWhiteSpace(servicePrincipalSecret))
                {
                    string envSecondaryServicePrincipal = Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION_2");

                    if (String.IsNullOrWhiteSpace(envSecondaryServicePrincipal) || !File.Exists(envSecondaryServicePrincipal))
                    {
                        envSecondaryServicePrincipal = Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION");
                    }

                    servicePrincipalClientId = Utilities.GetSecondaryServicePrincipalClientID(envSecondaryServicePrincipal);
                    servicePrincipalSecret = Utilities.GetSecondaryServicePrincipalSecret(envSecondaryServicePrincipal);
                }

                //=============================================================
                // Create a Kubernetes cluster

                Utilities.Log("Creating a Kubernetes cluster with one agent and one virtual machine");

                IKubernetesCluster kubernetesCluster = azure.KubernetesClusters.Define(aksName)
                    .WithRegion(region)
                    .WithNewResourceGroup(rgName)
                    .WithLatestVersion()
                    .WithRootUsername(rootUserName)
                    .WithSshKey(sshPublicKey)
                    .WithServicePrincipalClientId(servicePrincipalClientId)
                    .WithServicePrincipalSecret(servicePrincipalSecret)
                    .DefineAgentPool("ap")
                        .WithVirtualMachineSize(ContainerServiceVMSizeTypes.StandardD1V2)
                        .WithAgentPoolVirtualMachineCount(1)
                        .Attach()
                    .WithDnsPrefix("dns-" + aksName)
                    .Create();

                Utilities.Log("Created Kubernetes cluster: " + kubernetesCluster.Id);
                Utilities.Print(kubernetesCluster);

                //=============================================================
                // Updates a Kubernetes cluster agent with two virtual machines

                Utilities.Log("Updating the Kubernetes cluster agent with two virtual machines");

                kubernetesCluster.Update()
                    .WithAgentPoolVirtualMachineCount(2)
                    .Apply();

                Utilities.Log("Updated Kubernetes cluster: " + kubernetesCluster.Id);
                Utilities.Print(kubernetesCluster);
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);
                    azure.ResourceGroups.BeginDeleteByName(rgName);
                    Utilities.Log("Deleted Resource Group: " + rgName);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                //=============================================================
                // Authenticate
                var credentials = SdkContext.AzureCredentialsFactory.FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));
                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                // Print selected subscription
                Utilities.Log("Selected subscription: " + azure.SubscriptionId);

                RunSample(azure, credentials.ClientId, "aJ7NsOh7WG~X.jfEa_lfx7-VGJDH-oNA_r");
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }
}
