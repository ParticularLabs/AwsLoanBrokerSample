using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;

namespace Deploy;

class LambdaStack : Stack
{
    public LambdaStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        var servicePrincipal = new ServicePrincipal("lambda.amazonaws.com");
        var policyDocument = new PolicyDocument(new PolicyDocumentProps()
        {
            Statements =
            [
                new PolicyStatement(new PolicyStatementProps()
                {
                    Actions = ["sts:AssumeRole"],
                    Principals = [servicePrincipal],
                    Effect = Effect.ALLOW
                })
            ]
        });
        var roleProps = new RoleProps
        {
            AssumedBy = servicePrincipal
        };
        var role = new Role(this, "CreditCheckRole", roleProps);

        var lambdaPath = Path.GetFullPath(Path.Combine(System.Environment.CurrentDirectory, "..", "..", "..", "..", "lambdas"));
        var lambdaFn = new Function(this, "CreditCheck", new FunctionProps
        {
            Runtime = new Runtime("NODEJS_20_X"),
            Code = Code.FromAsset(lambdaPath),
            Handler = "creditbureau.score",
            Timeout = Duration.Seconds(30),
            Role = role
        });

        _ = new FunctionUrl(this, "CreditCheckUrl", new FunctionUrlProps
        {
            AuthType = FunctionUrlAuthType.NONE,
            Function = lambdaFn
        });

        _ = new Permission()
        {
            FunctionUrlAuthType = FunctionUrlAuthType.NONE,
            Principal = new AccountPrincipal("*"),
            Action = "lambda:InvokeFunctionUrl",
            Scope = lambdaFn
        };
    }
}
