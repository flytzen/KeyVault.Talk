using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace ShowSecrets
{
    class Program
    {
        private static string keyVaultUrl = "https://fl-test.vault.azure.net";
        private static string supersecretname = "supersecretpassword";  

        static async Task Main(string[] args)
        {
            var tokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
            
            var secretVersions = await keyVaultClient.GetSecretVersionsAsync(keyVaultUrl, supersecretname);

            foreach(var secretVersion in secretVersions)
            {
                var secretVersionValue = await keyVaultClient.GetSecretAsync(secretVersion.Id);
                Console.WriteLine($"{secretVersion.Identifier} | {secretVersion.Attributes.Enabled} | {secretVersion.Attributes.NotBefore} | {secretVersion.Attributes.Expires} | {secretVersionValue.Value}" );
            }  
        }
    }
}
