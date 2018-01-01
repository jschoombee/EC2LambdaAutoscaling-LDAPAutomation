using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Amazon.Lambda.Core;
using Novell.Directory.Ldap;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
//using System.Net.Http;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaLDAP
{

    public class Keys
    {
        public child Detail { get; set; }
    }
    public class child
    {
        public string EC2InstanceId { get; set; }
    }


    public class Function
    {
        //public void samplehandler(ILambdaContext context, Keys input) {
        //    input.Detail.
        //}

        static string version = "1.9";
        static string domain = System.Environment.GetEnvironmentVariable("domain");
        static string scope = System.Environment.GetEnvironmentVariable("scope");
        //static string privilegedDN = System.Environment.GetEnvironmentVariable("privilegedDN");
        static string SSMParameterPrivilegedDN = System.Environment.GetEnvironmentVariable("SSMParameterPrivilegedDN");
        static string SSMParameterPassword = System.Environment.GetEnvironmentVariable("SSMParameterPassword");
        static string ldapport = System.Environment.GetEnvironmentVariable("ldapPort");
        static string ldaps = System.Environment.GetEnvironmentVariable("ldaps");
        static int ldapPort = 0;
        // get the port to integer

       // AmazonSimpleSystemsManagementClient client = new AmazonSimpleSystemsManagementClient();


        public static async Task<GetCommandInvocationResponse> getoutput(AmazonSimpleSystemsManagementClient client, string commandId, string InstanceId)
        {

            GetCommandInvocationRequest req = new GetCommandInvocationRequest();
            req.CommandId = commandId;
            req.InstanceId = InstanceId;
            Task<GetCommandInvocationResponse> awaitedoutput = client.GetCommandInvocationAsync(req);
            return awaitedoutput.Result;
        }

        public static async Task<GetCommandInvocationResponse> GetComputer(AmazonSimpleSystemsManagementClient client, ILambdaContext context, string commandId, string InstanceId)
        {
            //context.Logger.LogLine(string.Format("About to Get Computer name from resultant commandId {0}", commandId));
            return await getoutput(client, commandId, InstanceId);
        }

        public static async Task<SendCommandResponse> SendCommand(string instanceid)
        {
             AmazonSimpleSystemsManagementClient client = new AmazonSimpleSystemsManagementClient();
            Dictionary<string, List<string>> parameters = new Dictionary<string, List<string>>();
            Amazon.SimpleSystemsManagement.Model.SendCommandRequest req = new SendCommandRequest();
            parameters.Add("commands", new List<string> { "$env:computerName" });
            req.Parameters = parameters;
            req.DocumentName = "AWS-RunPowerShellScript";
            req.InstanceIds = new List<string> { instanceid };
            req.Comment = "Pulled computer name via Lambda";

            Task<SendCommandResponse> SendCommandResponseTask;
            SendCommandResponseTask = client.SendCommandAsync(req);
            //return client.SendCommandAsync(req);
            return SendCommandResponseTask.Result;
        }

        public static async Task<SendCommandResponse> Go(ILambdaContext context, string InstanceId)
        {

            context.Logger.LogLine(string.Format("About to SendCommand to instanceId {0}..", InstanceId));
            return await SendCommand(InstanceId);
        }

        static void _ProcessComputerDeletion(string commandId, ILambdaContext context, string instanceId, string password, string privilegedDN) {
            AmazonSimpleSystemsManagementClient client = new AmazonSimpleSystemsManagementClient();
            int maxtries = 25;
            int count = 0;
            var computer = "";

            while (count < maxtries && string.IsNullOrEmpty(computer))
            {
                count++;
                Thread.Sleep(1350);
                context.Logger.LogLine(String.Format("Waiting on SSM stdout for computer name. In no results, will try for another {0} attempts ...", maxtries - count));
                Task<GetCommandInvocationResponse> resp = GetComputer(client, context, commandId, instanceId);

                if (!(string.IsNullOrEmpty(resp.Result.StandardOutputContent)))
                {
                    computer = resp.Result.StandardOutputContent.TrimEnd();
                    string ldapFilter = string.Format("(&(objectclass=computer)(name={0}))", computer);
                    context.Logger.LogLine(String.Format("ldapFilter to be used for LDAP search: {0}", ldapFilter));
                    //call trashComputer
                   
                    string result = RemoveComputerObjectfromLDAP(ldaps, domain, scope, privilegedDN, password, ldapPort, ldapFilter, context);
                    //string result = TrashComputer(input.domain, input.scope, input.privilegedDN, input.password, input.ldapPort, ldapFilter);
                    
                    context.Logger.LogLine(String.Format("Result from LDAP based Computer Deletion: {0}", result));
                }
                //else {
                //    string message = "No computer name returned via SSM for instance id: " + instanceId;
                //    //context.Logger.LogLine(message);
                //    throw new System.Exception(message);
                //}


            }
            if (string.IsNullOrEmpty(computer))
            {
                string message = "No computer name returned via SSM for instance id: " + instanceId;
                //context.Logger.LogLine(message);
                throw new System.Exception(message);
            }
        }


        public void FunctionHandler(Keys input, ILambdaContext context)
        {
            
            context.Logger.LogLine(string.Format("Starting Lambda Function: {0} with version {2} at {1}", context.FunctionName, DateTime.Now, version));
            //this async method will catch the timeout and print some troubleshooting steps
            Timeoutwaiter.LogTimeout(context.RemainingTime, context);

            AmazonSimpleSystemsManagementClient client = new AmazonSimpleSystemsManagementClient();
            
            string instanceId = input.Detail.EC2InstanceId;
            Int32.TryParse(ldapport, out ldapPort);

          //  context.Logger.LogLine("If the Function times out at this stage, check your Nat Gateway routing or if using a VPC Endpoint for EC2 Systems Manager - check the routing, subnet and conditional forwarding.");
            context.Logger.LogLine("Decrypting ssm parameter store values for LDAP Username DN.");
            string privilegedDN = SSMHelper.DecryptVal(SSMParameterPrivilegedDN);
            context.Logger.LogLine("Decrypting ssm parameter store values for LDAP Password DN.");
            string password = SSMHelper.DecryptVal(SSMParameterPassword);
            
           // context.Logger.LogLine(string.Format("Debug password: {0}", password));
            context.Logger.LogLine("Issuing SSM Send Command..");

            Task<SendCommandResponse> commandIdResult = Go(context, instanceId);

            context.Logger.LogLine(String.Format("Command ID is: {0}", commandIdResult.Result.Command.CommandId));
            context.Logger.LogLine(string.Format("About to Get Computer name from resultant commandId {0}", commandIdResult.Result.Command.CommandId));

            _ProcessComputerDeletion(commandIdResult.Result.Command.CommandId, context,  instanceId, password, privilegedDN);

        }

        public static string RemoveComputerObjectfromLDAP(string ldaps, string domain, string scope, string privilegedDN, string password, int ldapPort, string ldapFilter, ILambdaContext context)
        {

            string output = string.Format("No Such Computer Found using ldapFilter: {0}", ldapFilter);
            context.Logger.LogLine("LDAPOperation: About to open LDAP connection..");

            LdapConnection ldapConn = new LdapConnection();
            //if ldaps environment variable then enable secure socket layer
            if (ldaps == "true") {
                ldapConn.SecureSocketLayer = true;
                ldapConn.UserDefinedServerCertValidationDelegate += delegate { return true; };
            }
            try
            {
                ldapConn.Connect(domain, ldapPort);
                context.Logger.LogLine("LDAPOperation: About to bind to LDAP connection..");
                ldapConn.Bind(privilegedDN, password);
                context.Logger.LogLine(string.Format("LDAPOperation: Searching tree '{0}'..", scope));
                LdapSearchQueue queue = ldapConn.Search(scope,
                                   LdapConnection.SCOPE_SUB,
                                    ldapFilter,
                                    null,
                                    false,
                                   (LdapSearchQueue)null,
                                   (LdapSearchConstraints)null);
                LdapMessage message;


                while ((message = queue.getResponse()) != null)
                {
                    if (message is LdapSearchResult)
                    {


                        LdapEntry entry = ((LdapSearchResult)message).Entry;
                        context.Logger.LogLine(string.Format("Attempting deletion of object with DN: '{0}'", entry.DN));

                        try
                        {
                            ldapConn.Delete(entry.DN);
                            output = string.Format("Successfully deleted object with DN '{0}'", entry.DN);
                        }
                        catch (Novell.Directory.Ldap.LdapException NovellException)
                        {
                            output = string.Format("Failed to delete object, excepting with LDAP Exception message: {0}", NovellException.Message);
                            throw new System.Exception(output);
                        }
                        catch (Exception ex)
                        {
                            output = string.Format("Failed to delete object, excepting with general Exception message: {0}", ex.Message);
                            throw new System.Exception(output);
                        }

                        finally
                        {
                            ldapConn.Disconnect();

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //context.Logger.LogLine(string.Format("Ldap Bind Exception: " + ex.Message));
                //return ("The function failed to complete LDAP operation successfully. Check the logs.");
                throw new System.Exception(ex.Message);

            }
            if (output.Contains("No Such Computer Found")) { throw new Exception(output); } 
            return output;
        }

    }


}
