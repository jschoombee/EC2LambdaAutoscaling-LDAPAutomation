using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace LambdaLDAP
{
    public static class Timeoutwaiter
    {
        public static string WriteFile(string requestID)
        {

            var tempPath = System.IO.Path.GetTempPath();
            var logFile = System.IO.File.Create(tempPath + requestID);
            var logWriter = new System.IO.StreamWriter(logFile);
            logWriter.WriteLine("");
            logWriter.Dispose();
            return logFile.Name;

        }

        public static bool ShouldReturnTimeoutWarning(string logfile, int mstoExpire, ILambdaContext context) {

            if (System.IO.File.Exists(logfile) ) {
                DateTime lastwriteTime = System.IO.File.GetLastWriteTimeUtc(logfile);
                Double millisecondsElapsedSinceWrite = DateTime.UtcNow.Subtract(lastwriteTime).TotalMilliseconds;
                string[] stripped_reqId = logfile.Split('/');
                int lastItemIndex = stripped_reqId.GetUpperBound(0);
                string RequestID_FromFile = stripped_reqId[lastItemIndex];
                context.Logger.LogLine(string.Format("Debug|awsrequestID: {0} | RequestID_FromFile (from file): {1} | millisecondsElapsedSinceWrite: {2}", context.AwsRequestId, RequestID_FromFile, millisecondsElapsedSinceWrite));
                
                //if the last write time of the request id is greater than mstoExpire and if its not a new request id in same container
               if ( context.AwsRequestId != RequestID_FromFile && millisecondsElapsedSinceWrite > mstoExpire  ) { return true; }
               if ( context.AwsRequestId == RequestID_FromFile && millisecondsElapsedSinceWrite < 60000 ) { return true; }
               }
            
            return false;

        }
        public static async void LogTimeout(TimeSpan TimeRemaining, ILambdaContext context)
        {
            //log an error message 1 second with timeout
          
           // var timeToWait = System.Convert.ToInt32(Wait.Subtract(5000000).TotalMilliseconds);


            context.Logger.LogLine(string.Format("Started Timeout Checker Function with {0} remaining miliseconds", TimeRemaining.TotalMilliseconds));
            string requestIDTemp = WriteFile(context.AwsRequestId);


            int timeRemaininginms = System.Convert.ToInt32(TimeRemaining.TotalMilliseconds);
            //sleep for a second less than total timeout
            var lambdaTimeoutContext = Task.Delay(timeRemaininginms - 1000);
            await lambdaTimeoutContext;

            //throw new Exception("It looked liked the Lambda Function timed out. Check your Nat Gateway routing or if using a VPC Endpoint for EC2 Systems Manager - check the routing, subnet and DNS conditional forwarding.");
            //now lets see if the timestamp of the file was modified within the last few seconds, then we know this request has run before.
            if (ShouldReturnTimeoutWarning(requestIDTemp, 58000,context))
            {
                context.Logger.LogLine(String.Format("It looked like the request '{0}' will time out. Check your Nat Gateway routing or if using a VPC Endpoint for EC2 Systems Manager - check the routing, subnet and DNS conditional forwarding.", context.AwsRequestId));
            }
         
        }
    }
}
