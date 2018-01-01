using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace LambdaLDAP
{
    public class Helpers
    {

        //private static string Key1Value;
        // read values once, in the constructor
        public static string DecodePassword()
        {
            // Decrypt code should run once and variables stored outside of the function
            // handler so that these are decrypted once per container
            
            return DecodeEnvVar("password").Result;
        }
        private static async Task<string> DecodeEnvVar(string envVarName)
        {
            // retrieve env var text
            var encryptedBase64Text = Environment.GetEnvironmentVariable(envVarName);
            // convert base64-encoded text to bytes
            var encryptedBytes = Convert.FromBase64String(encryptedBase64Text);
            // construct client
            using (var client = new AmazonKeyManagementServiceClient())
            {
                // construct request
                var decryptRequest = new DecryptRequest
                {
                    CiphertextBlob = new MemoryStream(encryptedBytes),
                };
                // call KMS to decrypt data
                var response = await client.DecryptAsync(decryptRequest);
                using (var plaintextStream = response.Plaintext)
                {
                    // get decrypted bytes
                    var plaintextBytes = plaintextStream.ToArray();
                    // convert decrypted bytes to ASCII text
                    var plaintext = Encoding.UTF8.GetString(plaintextBytes);
                    return plaintext;
                }
            }
        }
        //public void FunctionHandler()
        //{
        //    Console.WriteLine("Encrypted environment variable Key1 = " + Key1Value);
        //}
    }
}