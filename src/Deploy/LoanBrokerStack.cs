using Amazon.CDK;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SQS;
using BankMessages;
using Deploy;

class LoanBrokerStack : Stack
{
    public LoanBrokerStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        _ = new NServiceBusEndpointResource(new EndpointDetails("LoanBroker") { EnableDynamoDBPersistence = true },
            this, "LoanBroker.LoanBroker");
        _ = new NServiceBusEndpointResource(new EndpointDetails("Client"), this, "LoanBroker.Client");
        var endpointDetails = new EndpointDetails("Bank1Adapter");
        endpointDetails.EventsToSubscribe = [typeof(QuoteRequested)];
        _ = new NServiceBusEndpointResource(endpointDetails, this, "LoanBroker.Bank1Adapter");
        _ = new NServiceBusEndpointResource(new EndpointDetails("Bank2Adapter"), this, "LoanBroker.Bank2Adapter");
        _ = new NServiceBusEndpointResource(new EndpointDetails("Bank3Adapter"), this, "LoanBroker.Bank3Adapter");

        _ = new Queue(this, "error", new QueueProps
        {
            QueueName = "error",
            RetentionPeriod = Duration.Seconds(900)
        });

        _ = new Function(this, "CreditScoreLambda", new FunctionProps
        {
            Code = Code.FromAsset(Path.Combine(Directory.GetCurrentDirectory(), "src/lambdas")),
            Runtime = Runtime.NODEJS_16_X,
            Handler = "creditbureau.score"
        });

        var cluster = new Cluster(this, "LoanBrokerCluster");

        _ = new ApplicationLoadBalancedFargateService(this, "Grafana",
            new ApplicationLoadBalancedFargateServiceProps
            {
                TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                {
                    Image = ContainerImage.FromRegistry("grafana/grafana-oss:latest"),
                    ContainerPort = 3000,
                },
                PublicLoadBalancer = true,
                Cluster = cluster
            });

        ApplicationContainer("Client", cluster);
        ApplicationContainer("Bank1Adapter", cluster);
        ApplicationContainer("Bank2Adapter", cluster);
        ApplicationContainer("Bank3Adapter", cluster);
        ApplicationContainer("LoanBroker", cluster);
    }

    private void ApplicationContainer(string applicationName, Cluster cluster)
    {
        var appImageAsset = new DockerImageAsset(this, applicationName + "Image", new DockerImageAssetProps
        {
            Directory = Path.Combine(Directory.GetCurrentDirectory(), "src"),
            File = applicationName + "/Dockerfile",
        });
        var env = new Dictionary<string, string>();
        //env.Add("LOCALSTACK_URL", "http://localhost.localstack.cloud:4566");
        env.Add("OTLP_METRICS_URL", "http://adot:5318/v1/metrics");
        env.Add("OTLP_TRACING_URL", "http://jaeger:4318/v1/traces");

        FargateTaskDefinition taskDefinition = new FargateTaskDefinition(this, applicationName + "TaskDef", new FargateTaskDefinitionProps
        {
            MemoryLimitMiB = 1024,
            Cpu = 512
        });


        taskDefinition.AddContainer(applicationName+"container", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromDockerImageAsset(appImageAsset),
            PortMappings = [],
            Environment = env,
            Secrets = new Dictionary<string, Secret>()
        });

        var fargateService = new FargateService(this, applicationName+"Service", new FargateServiceProps()
        {
            Cluster = cluster,
            TaskDefinition = taskDefinition
        });
    }
}