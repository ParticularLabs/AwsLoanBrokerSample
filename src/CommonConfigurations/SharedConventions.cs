using System.Diagnostics.Metrics;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using NLog.Extensions.Logging;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Extensions.Logging;
using NServiceBus.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace CommonConfigurations;

public static class SharedConventions
{
    public const string LocalStackEdgeDefaultUrl = "http://localhost:4566";
    public const string OtlpMetricsDefaultUrl = "http://localhost:5318/v1/metrics";
    public const string OtlpTracesDefaultUrl = "http://localhost:4318/v1/traces";
    public const string LocalStackEdgeEnvVar = "LOCALSTACK_URL";
    public const string OtlpMetricsUrlEnvVar = "OTLP_METRICS_URL";
    public const string OtlpTracesUrlEnvVar = "OTLP_TRACING_URL";
    public static readonly AWSCredentials EmptyLocalStackCredentials = new BasicAWSCredentials("xxx", "xxx");
    public static readonly Meter LoanBrokerMeter = new ("LoanBroker", "0.1.0");

    public static string LocalStackUrl() => Environment.GetEnvironmentVariable(LocalStackEdgeEnvVar) ?? LocalStackEdgeDefaultUrl;

    public static RoutingSettings UseCommonTransport(this EndpointConfiguration endpointConfiguration)
    {
        var url = LocalStackUrl();
        var sqsConfig = new AmazonSQSConfig { ServiceURL = url };
        var snsConfig = new AmazonSimpleNotificationServiceConfig { ServiceURL = url };

        var transport = new SqsTransport(
            new AmazonSQSClient(EmptyLocalStackCredentials, sqsConfig),
            new AmazonSimpleNotificationServiceClient(EmptyLocalStackCredentials, snsConfig));
        return endpointConfiguration.UseTransport(transport);
    }

    public static void CommonEndpointSetting(this EndpointConfiguration endpointConfiguration)
    {
        // disable diagnostic writer to prevent docker errors
        // in production each container should map a volume to write diagnostic
        endpointConfiguration.CustomDiagnosticsWriter((_, _) => Task.CompletedTask);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();
        EnableMetrics(endpointConfiguration);
        EnableTracing(endpointConfiguration);
    }


    static void EnableMetrics(EndpointConfiguration endpointConfiguration)
    {
        var endpointName = endpointConfiguration.GetSettings().EndpointName();
        var attributes = new Dictionary<string, object>
        {
            ["service.name"] = endpointName,
            ["service.instance.id"] = Guid.NewGuid().ToString(),
        };

        var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(attributes);

        Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("NServiceBus.Core")
            .AddMeter("LoanBroker")
            .AddOtlpExporter(cfg =>
            {
                var url = Environment.GetEnvironmentVariable(OtlpMetricsUrlEnvVar) ?? OtlpMetricsDefaultUrl;
                cfg.Endpoint = new Uri(url);
                cfg.Protocol = OtlpExportProtocol.HttpProtobuf;
            })
            .Build();

        endpointConfiguration.EnableOpenTelemetry();
    }

    static void EnableTracing(EndpointConfiguration endpointConfiguration)
    {
        var endpointName = endpointConfiguration.GetSettings().EndpointName();

        var attributes = new Dictionary<string, object>
        {
            ["service.name"] = endpointName,
            ["service.instance.id"] = Guid.NewGuid().ToString(),
        };

        var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(attributes);

        Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("NServiceBus.Core")
            .AddOtlpExporter(cfg =>
            {
                var url = Environment.GetEnvironmentVariable(OtlpTracesUrlEnvVar) ?? OtlpTracesDefaultUrl;
                cfg.Endpoint = new Uri(url);
                cfg.Protocol = OtlpExportProtocol.HttpProtobuf;
            })
            .Build();

        endpointConfiguration.EnableOpenTelemetry();
    }

    public static void ConfigureMicrosoftLoggingIntegration()
    {
        // Integrate NServiceBus logging with Microsoft.Extensions.Logging
        ILoggerFactory extensionsLoggerFactory = new NLogLoggerFactory();
        LogManager.UseFactory(new ExtensionsLoggerFactory(extensionsLoggerFactory));
    }
}