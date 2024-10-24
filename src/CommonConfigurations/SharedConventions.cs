using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using NServiceBus.Extensions.Logging;
using NServiceBus.Logging;

namespace CommonConfigurations;

public record Customizations(EndpointConfiguration EndpointConfiguration, RoutingSettings Routing);

public static class SharedConventions
{
    public static HostApplicationBuilder ConfigureAwsNServiceBusEndpoint(this HostApplicationBuilder builder,
        string endpointName, Action<Customizations>? customize = null)
    {
        ConfigureMicrosoftLoggingIntegration();

        var endpointConfiguration = new EndpointConfiguration(endpointName);

        // Configure SQS Transport
        var transport = new SqsTransport();
        var routing = endpointConfiguration.UseTransport(transport);

        // Configure DynamoDB Persistence
        var persistence = endpointConfiguration.UsePersistence<DynamoPersistence>();
        persistence.Sagas().UsePessimisticLocking = true;

        //The Deploy project will create it through the AWS CDK
        persistence.DisableTablesCreation();
        persistence.UseSharedTable(new TableConfiguration
        {
            TableName = "LoanBroker.NServiceBus.Storage"
        });
        SetCommonEndpointSettings(endpointConfiguration);

        // Endpoint-specific customization
        customize?.Invoke(new Customizations(endpointConfiguration, routing));

        builder.UseNServiceBus(endpointConfiguration);
        return builder;
    }


    static void SetCommonEndpointSettings(EndpointConfiguration endpointConfiguration)
    {
        // disable diagnostic writer to prevent docker errors
        // in production each container should map a volume to write diagnostic
        endpointConfiguration.CustomDiagnosticsWriter((_, _) => Task.CompletedTask);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableOutbox();
        //endpointConfiguration.EnableInstallers();

        endpointConfiguration.EnableOpenTelemetryMetrics();
        endpointConfiguration.EnableOpenTelemetryTracing();

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

    public static void DisableRetries(this EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.Recoverability()
            .Immediate(customize => customize.NumberOfRetries(0))
            .Delayed(customize => customize.NumberOfRetries(0));
    }

    public static void ConfigureMicrosoftLoggingIntegration()
    {
        // Integrate NServiceBus logging with Microsoft.Extensions.Logging
        var nlog = new NLogLoggerFactory();
        LogManager.UseFactory(new ExtensionsLoggerFactory(nlog));
    }
}