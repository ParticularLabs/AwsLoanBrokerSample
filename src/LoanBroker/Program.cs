using CommonConfigurations;
using LoanBroker.Messages;
using LoanBroker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

SharedConventions.ConfigureMicrosoftLoggingIntegration();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ICreditScoreProvider>(_ => new HTTPCreditScoreProvider());

builder.Services.AddSingleton<IQuoteAggregator, BestRateQuoteAggregator>();

var endpointConfiguration = new EndpointConfiguration("LoanBroker");
endpointConfiguration.CommonEndpointSetting();
var routingSettings = endpointConfiguration.UseCommonTransport();
routingSettings.RouteToEndpoint(typeof(FindBestLoanWithScore), "LoanBroker");

var persistence = endpointConfiguration.UsePersistence<DynamoPersistence>();
persistence.Sagas().UsePessimisticLocking = true;

//The Deploy project will create it through the AWS CDK
persistence.DisableTablesCreation();
persistence.UseSharedTable(new TableConfiguration
{
    TableName = "LoanBroker.NServiceBus.Storage"
});

endpointConfiguration.EnableOutbox();

builder.UseNServiceBus(endpointConfiguration);

var app = builder.Build();
app.Run();