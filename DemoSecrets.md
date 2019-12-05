
# Secrets Demo Script

## Prepare
```
az login
az account set --subscription xxxx
az configure --scope local --defaults location=westeurope group=fl-test-keyvault
az group create --name fl-test-keyvault
```

## Create a Key Vault
*Run this ahead of time - it takes time*
```
az keyvault create --name fl-test
```

## Give read and write permissions to a user
```
az keyvault set-policy `
    --name fl-test `
    --upn "flytzen@neworbit.co.uk" `
    --secret-permissions get, list, set  
```
In the real world, a consuming application should not have "set" and an admin shouldn't have get

## Add a secret
*You can do this in the portal as well - and in code of course*
```
az keyvault secret set `
    --name supersecretpassword `
    --vault-name fl-test `
    --value ohmyworditssosecret `
    --not-before 2019-12-03T14:30:25z `
    --expires 2019-12-24T23:59:59z `
    --disabled false
    --output table
```
Note that the "disabled" flag is enforced; most operations are blocked on a disabled secret.
However, the nbf and expiry are for information and it's up to you to read them and act on them
This is *different* for keys!s

## Set up a console app to read the secret
*Note: New packages have just come out but they have less intuitive auth - for now*
```
cd C:\Code\KeyVault.Talk
dotnet new console -n ShowSecrets 
cd ShowSecrets 
dotnet add package Microsoft.Azure.KeyVault
dotnet add package Microsoft.Azure.Services.AppAuthentication
code .
```

## Variables
```csharp
private static string keyVaultUrl = "https://fl-test.vault.azure.net";
private static string supersecretname = "supersecretpassword";
```

## Simple program to retrieve secret
```csharp
static async Task Main(string[] args)
{
    var tokenProvider = new AzureServiceTokenProvider();
    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
    var secretValue = await keyVaultClient.GetSecretAsync(keyVaultUrl, supersecretname);

    Console.WriteLine(secretValue.Value);
}

```

When running, add 
```json
"logging": {
    "moduleLoad": false
}
```
to `launch.json`


## Change the secret
```
az keyvault secret set `
    --name supersecretpassword `
    --vault-name fl-test `
    --value OXFORD `
    --not-before 2019-12-24T14:30:25z `
    --expires 2019-12-31T23:59:59z `
    --disabled false
```


## See all the versions
```csharp
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
```

```
dotnet new mvc
dotnet add package Microsoft.Azure.KeyVault
dotnet add package Microsoft.Azure.Services.AppAuthentication
dotnet add package Microsoft.Extensions.Configuration.AzureKeyVault
```

In `program.cs` change:
```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        })
        .ConfigureAppConfiguration((context, config) => {
            config.AddAzureKeyVault("https://fl-test.vault.azure.net",
                                    new KeyVaultClient(
                                        new KeyVaultClient.AuthenticationCallback(
                                            new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                                    new DefaultKeyVaultSecretManager());
        });

```

In `Home/index.cshtml` add:
```
@using Microsoft.Extensions.Configuration
@inject IConfiguration Configuration
@{
   string myPassword = Configuration["supersecretpassword"];
}

<h2>Retrieved from Key Vault:</h2>
<h1>@myPassword</h1>
```

# Do it on Azure
Basically, deploy it, create the Managed Identity and give the Managed Identity read rights to the secrets.
Job done.
