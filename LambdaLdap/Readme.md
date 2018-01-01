# Sample EC2 terminate instance lifecylce hook event

{
  "version": "0",
  "id": "468fe059-f4b7-445f-bb22-2a271b94974d",
  "detail-type": "EC2 Instance-terminate Lifecycle Action",
  "source": "aws.autoscaling",
  "account": "123456789012",
  "time": "2015-12-22T18:43:48Z",
  "region": "us-east-1",
  "resources": [
    "arn:aws:autoscaling:us-east-1:123456789012:autoScalingGroup:59fcbb81-bd02-485d-80ce-563ef5b237bf:autoScalingGroupName/sampleASG"
  ],
  "detail": {
    "LifecycleActionToken": "630aa23f-48eb-45e7-aba6-799ea6093a0f",
    "AutoScalingGroupName": "sampleASG",
    "LifecycleHookName": "SampleLifecycleHook-6789",
    "EC2InstanceId": "i-06fed45846226be3f",
    "LifecycleTransition": "autoscaling:EC2_INSTANCE_TERMINATING"
  }
}

# AWS Lambda Empty Function Project

This starter project consists of:
* Function.cs - class file containing a class with a single function handler method
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS
* project.json - .NET Core project file with build and tool declarations for the Amazon.Lambda.Tools Nuget package

You may also have a test project depending on the options selected.

The generated function handler is a simple method accepting a string argument that returns the uppercase equivalent of the input string. Replace the body of this method, and parameters, to suit your needs. 

## Here are some steps to follow from Visual Studio:

To deploy your function to AWS Lambda, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed function open its Function View window by double-clicking the function name shown beneath the AWS Lambda node in the AWS Explorer tree.

To perform testing against your deployed function use the Test Invoke tab in the opened Function View window.

To configure event sources for your deployed function, for example to have your function invoked when an object is created in an Amazon S3 bucket, use the Event Sources tab in the opened Function View window.

To update the runtime configuration of your deployed function use the Configuration tab in the opened Function View window.

To view execution logs of invocations of your function use the Logs tab in the opened Function View window.

## Here are some steps to follow to get started from the command line:

Once you have edited your function you can use the following command lines to build, test and deploy your function to AWS Lambda from the command line (these examples assume the project name is *EmptyFunction*):

Restore dependencies
```
    cd "LambdaLDAP"
    dotnet restore
```

Execute unit tests
```
    cd "LambdaLDAP/test/LambdaLDAP.Tests"
    dotnet test
```

Deploy function to AWS Lambda
```
    cd "LambdaLDAP/src/LambdaLDAP"
    dotnet lambda deploy-function
```
