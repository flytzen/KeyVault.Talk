using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Azure.Services.AppAuthentication;

namespace KeyDemo
{
    class Program
    {
        private static KeyVaultClient keyVaultClient;
        private static string keyVaultUrl = "https://fl-test.vault.azure.net/";
        private static string keyName = "testkey";
        
        static async Task Main(string[] args)
        {
            var tokenProvider = new AzureServiceTokenProvider();
            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
            
            var signedData = await Sign(inputText);
            
            var verifyResult = await Verify(signedData);
            
            Console.WriteLine($"\nVerify result: {verifyResult}.\n\nKey identifier: {signedData.SigningKeyIdentifier}\n");
        }

        private static async Task<EncryptedData> EncryptAndWrap(string textToEncrypt)
        {
            // NOTE: This is simplified in several ways. Like you should work with byte arrays/streams, 
            // not text due to encoding vulnerabilities and you 
            // should think about "Authenticated Encryption" for padding etc. 
            // Read SecurityDriven.Net for details.

            var encryptedData = new EncryptedData();
            encryptedData.EncryptedDataStream = new MemoryStream();  // Bad stream design, just for illustration

            // This creates a random key and initialisation vector (IV) and encrypts the data
            using (var encryptingAes = Aes.Create())
            {
                encryptedData.InitialisationVector = encryptingAes.IV;
                var encryptor = encryptingAes.CreateEncryptor();
                using (var encryptingStream = new CryptoStream(encryptedData.EncryptedDataStream, encryptor, CryptoStreamMode.Write, true)) 
                using (var writer = new StreamWriter(encryptingStream)) // NOTE: This is a text writer! Shouldn't do this if we're dealing with binary data!
                {
                    writer.Write(inputText);
                    writer.Flush();
                    encryptingStream.Flush();
                }

                // ** THIS IS WHERE WE USE KEY VAULT **
                var wrappingResult = await keyVaultClient.WrapKeyAsync($"{keyVaultUrl}keys/{keyName}", 
                                            JsonWebKeyEncryptionAlgorithm.RSA15, 
                                            encryptingAes.Key);

                encryptedData.WrappedKey = wrappingResult.Result; // Save the encrypted key so we can use it later
                encryptedData.WrappingKeyIdentifier = wrappingResult.Kid; // Save which version of the symmetric key we used
                // Note: Because this uses the Public Key, you can actually retrieve the public key from Key Vault and do 
                // this locally for performance reasons, if you do a lot of these.
            }

            encryptedData.EncryptedDataStream.Position = 0;

            return encryptedData;
        }


        private static async Task<string> Decrypt(EncryptedData data)
        {
            // *** Ask Key Vault to decrypt the symmetric key ****
            var unwrapKeyResult = await keyVaultClient.UnwrapKeyAsync(data.WrappingKeyIdentifier, 
                                                        JsonWebKeyEncryptionAlgorithm.RSA15, 
                                                        data.WrappedKey);
            var symmetricKey = unwrapKeyResult.Result;

            using (var decryptingAes = Aes.Create()) 
            {
               decryptingAes.IV = data.InitialisationVector;
               decryptingAes.Key = symmetricKey;
               var decryptor = decryptingAes.CreateDecryptor();
               
               using (var decryptingStream = new CryptoStream(data.EncryptedDataStream, decryptor, CryptoStreamMode.Read))
               using (var reader = new StreamReader(decryptingStream))
               {
                   return reader.ReadToEnd(); // There are some dangers in uncritically returning text - this is simplified!
               }
            }
        }


        private static async Task<SignedData> Sign(string text) 
        {
            // In the real world, you'd probably generate a hash of an object 
            // rather than passing in a string - or pass in a stream or a byte array
            // Create a normal *hash/digest* that we can then sign (encrypt with the public key)
            var digest = GetSHA512Digest(text);

            // This will use the latest key version
            // Note that the type of digest (SHA512 here) determines which type of signature algo you should use!
            var signatureResult = await keyVaultClient.SignAsync($"{keyVaultUrl}keys/{keyName}",
                                        JsonWebKeySignatureAlgorithm.RS512, 
                                        digest);

            var signedData = new SignedData {
                SigningKeyIdentifier = signatureResult.Kid,
                Signature = signatureResult.Result,
                Data = text
            };
            return signedData;
        }

        private static async Task<bool> Verify(SignedData data)
        {
            // You could retrieve the public key and do this locally
            var newDigest = GetSHA512Digest(data.Data);
            var verifyResult = await keyVaultClient.VerifyAsync(data.SigningKeyIdentifier, 
                                            JsonWebKeySignatureAlgorithm.RS512, 
                                            newDigest, 
                                            data.Signature);
            return verifyResult;
        }

        private static byte[] GetSHA512Digest(string input)
        {
            using (var sha = SHA512.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input); 
                return sha.ComputeHash(bytes);
            }
        }

        private static string inputText = @"AzureServiceTokenProvider will use the developer's security context to get a token to authenticate to Key Vault. This removes the need to create a service principal, and share it with the development team. It also prevents credentials from being checked in to source code. AzureServiceTokenProvider will use Azure CLI or Active Directory Integrated Authentication to authenticate to Azure AD to get a token. That token will be used to fetch the secret from Azure Key Vault.
Azure CLI will work if the following conditions are met:
You have Azure CLI 2.0 installed. Version 2.0.12 supports the get-access-token option used by AzureServiceTokenProvider. If you have an earlier version, please upgrade.
You are logged into Azure CLI. You can login using az login command.
Azure Active Directory Authentication will only work if the following conditions are met:
Your on-premise active directory is synced with Azure AD.
You are running this code on a domain joined machine.
Since your developer account has access to the Key Vault, you should see the secret on the web page. Principal Used will show type User and your user account.
You can also use a service principal to run the application on your local development machine. See the section Running the application using a service principal later in the tutorial on how to do this.";
    }
}
