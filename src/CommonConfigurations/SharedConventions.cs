using System.Diagnostics.Metrics;
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
    public const string OtlpMetricsDefaultUrl = "http://localhost:5318/v1/metrics";
    public const string OtlpTracesDefaultUrl = "http://localhost:4318/v1/traces";
    public const string OtlpMetricsUrlEnvVar = "OTLP_METRICS_URL";
    public const string OtlpTracesUrlEnvVar = "OTLP_TRACING_URL";
    public static readonly Meter LoanBrokerMeter = new("LoanBroker", "0.1.0");

    public static RoutingSettings UseCommonTransport(this EndpointConfiguration endpointConfiguration)
    {
        var transport = new SqsTransport();
        return endpointConfiguration.UseTransport(transport);
    }

    public static void CommonEndpointSetting(this EndpointConfiguration endpointConfiguration)
    {
        // disable diagnostic writer to prevent docker errors
        // in production each container should map a volume to write diagnostic
        endpointConfiguration.CustomDiagnosticsWriter((_, _) => Task.CompletedTask);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        //TODO: remove installers, taking into account that we also need to support deploying to LocalStack
     //   endpointConfiguration.EnableInstallers();

        EnableMetrics(endpointConfiguration);
        EnableTracing(endpointConfiguration);

        endpointConfiguration.ConnectToServicePlatform(new ServicePlatformConnectionConfiguration
        {
            Heartbeats = new()
            {
                Enabled = true,
                HeartbeatsQueue = "Particular-ServiceControl",
            },
            CustomChecks = new()
            {
                Enabled = true,
                CustomChecksQueue = "Particular-ServiceControl"
            },
            ErrorQueue = "error",
            SagaAudit = new()
            {
                Enabled = true,
                SagaAuditQueue = "audit"
            },
            MessageAudit = new()
            {
                Enabled = true,
                AuditQueue = "audit"
            },
            Metrics = new()
            {
                Enabled = true,
                MetricsQueue = "Particular-Monitoring",
                Interval = TimeSpan.FromSeconds(1)
            }
        });
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
            .AddMeter("NServiceBus.Core.Pipeline.Incoming")
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