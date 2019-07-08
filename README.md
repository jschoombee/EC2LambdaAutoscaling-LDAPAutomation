# EC2LambdaAutoscaling-LDAPAutomation
EC2 Autoscaling automatic domain join and removal using Lifecycle Hooks and AWS Lambda.
# The Problem
Depending on the elasticity of your auto scaling group(s), this can typically leave a large trail of computer objects behind. Retroactive removal of computer objects from a directory is never without risk; typically scheduled scripts scan computer objects for objects with old computer passwords, often coupled with DNS and/or ICMP (ping) checks to sanitize; the issue comes in with computer objects that don’t update their passwords (pwdLastset), usually this happens automatically every 30 days and is driven by the computer client. In turn this could skew the deletion targets for scripted runs and render a live server with a broken secure channel with its domain controller.
# The Solution
Based on a previous AWS Security blog that highlights steps to domain join your EC2 instances in an Auto-Scaling group to your Active Directory environment, this automates the entire process using CloudFormation for the lifecycle on the EC2 instance in the Auto Scaling Group, including the feature to remove the computer object from Directory Services when the group scales-in and terminates. Using LifeCycle Hooks, when an auto-scaling group scales-in, the termination event is paused which allows us to perform below lifecycle logic. 

The Lifecycle hook raises an instance event CloudWatch event source data as the trigger, a NET Core based Lambda function interrogates the instance for its computer name using a EC2 Systems Manager PowerShell-based Run Command while in its transition state. Finalizing the workflow in searching the LDAP(s) server for the computer object and using the LDAP delete operation to remove it.
1.	The AWS CloudFormation template launches the core framework, which includes a suite of microservices (AWS Lambda functions) that manage triggering LDAP operations for removal, EC2 Systems Manager document creation for domain join operations, Auto-scaling group and launch configuration creation.
2.	The Solution-generated AWS CloudFormation template configures this service based on parameters you define, and the roles necessary to perform actions across accounts. 

# Dependencies
Auto Scaling Group and Lambda VPC Dependencies:
1.	Subnet(s) for Lambda function - AWS Lambda uses a NET core based function which communicates with EC2 Simple Systems Manager as well as your Directory Servers in your VPC. For Lambda subnet(s), we have two options, either we need to make use of a NAT Gateway or a VPC Endpoint for EC2 Systems Manager. If you don’t already have a NAT Gateway in production, you can use a VPC Endpoint as it is cheap as chips.
Subnet(s) for the Auto Scaling Group – need to have their corresponding route table(s) route to a NAT Gateway, Internet Gateway or Virtual Private Gateway; only if using Option 2 below, or if you have another automation joining your instances to the your LDAP based Directory, then these subnets can be private.
If using a VPC Endpoint:
This guide will assist in the configuration. If not using Route 53 for DNS but using a Directory Services based DNS, make sure your DNS server has a conditional forwarder that resolves ssm.<region-code>.amazonaws.com (i.e. ssm.us-east-1.amazonaws.com) to the Amazon Provided DNS server.

If using a NAT Gateway:
Ensure your Lambda Function uses a subnet with a route table that uses a NAT Gateway as its default (0.0.0.0/0) route. Also ensure that the subnet route table of the Nat Gateway itself uses either an internet gateway or virtual gateway.

Note – Record the subnet-ids and availability zones before launching the solution; these subnets can be shared between the Auto-scaling Group and Lambda function. Also check that the subnets are routable to your directory servers.

A walkthrough example guide can be found here https://gist.github.com/reggi/dc5f2620b7b4f515e68e46255ac042a7
Best practice – create or use at least two subnets, one per availability zone for redundancy.


2.	Security Groups for Lambda and Auto-scaling Group Launch Configuration If you have a Launch configuration already, then you can re-use the security group. Again it is possible to use the same security group for both the launch configuration and the Lambda function, just check that the inbound rule allows for all inbound traffic from the same security group ID (sg-xxxxxxxx). 

Note – Record the security group IDs for both the Lambda Function and the launch configuration before launching the solution

3.	DHCP Options Set – should be configured for your VPC where the Auto-scaling group instances and the Lambda function reside. They should point to your directory IP addresses or to the production DNS server IP addresses that can resolve your domain name. Without this the Lambda function won’t be able to resolve the DNS name of your domain to remove the computer objects.


Directory and LDAP Dependencies:
At this time the solution supports LDAP with limited support for LDAPS (encrypted LDAP transport). If you choose LDAPS, note that the SSL certificate common name and certificate authority/issuer chain won’t be validated/traversed however, the network transport will be encrypted and the LDAP bind operations will be encrypted using StartTLS. 

Ensure you have the following information ready before launching the stack:

1.	Ldap Port (usually 389 for LDAP and 636 for LDAPS)
2.	Directory ID of your simple, managed or AD connector
3.	IP; addresses of your Directory Servers
4.	Directory FQDN (dc=mydomain,dc=com)
5.	Distinguished Name for where to create computer accounts, i.e. cn=computers,dc=mydomain,dc=com
6.	LDAP Base DN for Search, i.e. dc=mydomain,dc=com
7.	LDAP Administrative user to remove computer object, i.e. 
CN=Admin,OU=Users,OU=mydomain,DC=mydomain,DC=com
8.	LDAP Administrative user password
9.	Existing IAM role that is able to decrypt/encrypt the administrator password in KMS

Launch CloudFormation Stack - https://console.aws.amazon.com/cloudformation/home?#/stacks/new?templateURL=https://s3.amazonaws.com/lamfuncs/master.template.yml

# Option 2 - I don’t need the entire solution, I just want to incorporate computer object removal with my existing domain-joined Auto Scaling Group(s) 
Can I do this manually? I just want to incorporate computer object removal with my existing domain-joined Auto Scaling Group(s)
You may have a use case to make use of the Lambda Function to remove the computer objects from the Auto Scaling Group, either by means of exposing the function via API Gateway or if you have a specific configuration that calls for this. See the Dependencies above before continuing and make sure the AWS CLI is installed.

# Step 1 - Configure Lambda and KMS
1. Download the deployment package.
2. Download the IAM policy -- https://s3.amazonaws.com/lamfuncs/iampolicy.txt
3. Create an IAM Role, Select Lambda then Next: Permissions
4. Click Create Policy button, the JSON Tab and paste the contents of IAM policy in Step 2, then click the Review Policy button
5. Name the Policy, click on Create Policy and take note of the name.
6. Once created, Go back to the Create Role browser Tab, click the Refresh button, search for the policy created in Step 4, and tick the box on the left policy then click Next:Review
7. Give the IAM Role a Name, verify the Policies below shows your newly created policy and click on Create Role. Note the arn of the role once created for Step 6 below.
8. Verify the CLI is configured then in the AWS CLI run the following (Note the name of the arn of the created function):
‘aws lambda create-function --function-name <Name-of-function> --runtime dotnetcore1.0 --role <Name-of-arn> --handler LambdaLdap::LambdaLDAP.Function::FunctionHandler --zip-file fileb://c:/<Name of Downloaded zip file Goes Here>.zip –timeout 60'
9. Open the IAM console and select Encryption keys
10. Select Get Started the Region where the Lambda Function was created in Step 7
11. Click on Create Key and give it an Alias and click on Next Step, add the administrator(s) and or IAM Roles that can administer the key and click on Next Step then Finish
12. Click on the Key alias, under the Key Users section, add the IAM role name created in step 6 above as a Key Use and click on the Attach Button
13. Create the SSM Parameter with an example name of myLdapSSMParameterPrivilegedDN and select Secure String; enter the Distinguished Name of the administrative user that can manage computer accounts in your LDAP Directory, select the KMS Key ID from the steps above.
14. Create the SSM Parameter with an example name of myLdapSSMParameterPassword and select Secure String; enter the password of administrative user, select the KMS Key ID from the steps above.
16. Open the Lambda Console and open the Function that you created
17. Make sure you select subnets and security groups in the VPC as per Dependencies above.
18. Click on Environment Variables and Create the Key/Value pairs as per below (using your values as per the prerequisites (note the Keys are case-sensitive):
Key: SSMParameterPrivilegedDN
Example Value: myLdapSSMParameterPrivilegedDN

Key: SSMParameterPassword
Example Value: myLdapSSMParameterPassword

Key: domain
Example Value: eu-west-2.domain.com

Key: scope (The search base to look for the computer object. The root is normally safest but can be a slightly more ‘expensive’ LDAP search operation, set this as granular as possible (i.e. where the auto-scaling computer objects reside created). It will by default perform a subtree search)
Example Value: DC=eu-west-2,DC=domain,DC=com

Key: ldapPort (Use 389 for LDAP and 636 for LDAPS. Note LDAPS will attempt to connect using the dns name of the domain above and does not validate certificate chains More info: How to enable LDAPS on AWS directory)
Example Value: 389

Key: ldaps (true or false)
Example Value: true

# Step 2 - Configure the LifeCycle Hook and CloudWatch Event Rule
1. Open the Auto Scaling Group Console and select your Auto Scaling group
2. Under the LifeCycle Hooks Tab, click on the Create Lifecycle Hook
3. Under LifeCycle Transition, Give it a Name, select Instance Terminate under the Lifecycle Transition drop-down, give it a Heartbeat Timeout of 300 seconds and a Default Result of CONTINUE (we don’t want the instance termination to fail because of a failed LDAP computer deletion)
4. Create a CloudWatch Event Rule in the console, click on the Create Rule button ensure the Event Pattern radio button is selected, for Service Name choose Auto Scaling, Event Type Instance Launch and terminate, the select the Specific instance event(s) radio button and select EC2 Instance-terminate Lifecycle Action. Select the Specific group name(s) radio button and select your Auto scaling group
5. Under the Targets navigation bar on the right, select Add Target and select the Lambda Function you created in Step 6
