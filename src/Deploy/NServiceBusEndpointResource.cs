using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SQS;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace Deploy;

public class NServiceBusEndpointResource : Resource
{
    public NServiceBusEndpointResource(EndpointDetails endpoint, Construct scope, string id,
        IResourceProps? props = null)
        : base(scope, id, props)

    {
        _ = new Queue(this, endpoint.EndpointName, new QueueProps
        {
            QueueName = endpoint.FullQueueName,
            RetentionPeriod = Duration.Seconds(endpoint.RetentionPeriod.TotalSeconds)
        });

        _ = new Queue(this, $"{endpoint.EndpointName}-delay", new QueueProps
        {
            QueueName = endpoint.DelayQueueName,
            Fifo = true,
            DeliveryDelay = Duration.Seconds(900),
            RetentionPeriod = Duration.Seconds(endpoint.RetentionPeriod.TotalSeconds)
        });

        if (endpoint.EnableDynamoDBPersistence)
        {
            _ = new Table(this, $"{endpoint.EndpointName}-storage", new TableProps()
            {
                TableName = $"{endpoint.EndpointName}.NServiceBus.Storage",
                PartitionKey = new Attribute(){ Name = "PK",Type = AttributeType.STRING },
                SortKey = new Attribute(){ Name = "SK",Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY
            });
        }

        if (endpoint.EventsToSubscribe != null)
        {
            foreach (var evtType in endpoint.EventsToSubscribe)
            {
                //TODO: create the needed topic and subscribe
            }
        }
    }
}

public class EndpointDetails(string endpointName)
{
    public string EndpointName => endpointName;
    public string? Prefix { get; set; }

    public string FullQueueName => $"{Prefix}{endpointName}";

    public string DelayQueueName => $"{FullQueueName}-delay.fifo";

    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(4);

    public Type[]? EventsToSubscribe { get; set; }

    public bool EnableDynamoDBPersistence { get; set; }
}
