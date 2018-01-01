using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
//using System.Threading;
using System.Threading.Tasks;

namespace LambdaLDAP
{
    public static class SSMHelper
 
    {
        public static AmazonSimpleSystemsManagementClient returnClient() {
            
            AmazonSimpleSystemsManagementClient client = new AmazonSimpleSystemsManagementClient();
     
            return client;
        }

        public static async Task<GetParameterResponse> SSMParamStore(string Name)
        {
            AmazonSimpleSystemsManagementClient client = new AmazonSimpleSystemsManagementClient();

            GetParameterRequest param = new GetParameterRequest();

            param.WithDecryption = true;
            param.Name = Name;
            Task<GetParameterResponse> response =   client.GetParameterAsync(param);
            return response.Result;


        }

        public static string DecryptVal(string envVarName)
        {
            var res = SSMParamStore(envVarName).Result;
            return res.Parameter.Value;
        }

        public static async Task<GetParameterResponse> testawait(string Name)
        {
            AmazonSimpleSystemsManagementClient client = new AmazonSimpleSystemsManagementClient();

            GetParameterRequest param = new GetParameterRequest();

            param.WithDecryption = true;
            param.Name = Name;
            var response = await client.GetParameterAsync(param);
            return response;


        }


    }
}
