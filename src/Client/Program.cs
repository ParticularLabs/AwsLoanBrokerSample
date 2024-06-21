using ClientMessages;
using CommonConfigurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

SharedConventions.ConfigureMicrosoftLoggingIntegration();

var builder = Host.CreateApplicationBuilder(args);

// TODO: consider moving common endpoint configuration into a shared project
// for use by all endpoints in the system

var endpointConfiguration = new EndpointConfiguration("Client");

endpointConfiguration.CommonEndpointSetting();
var routingSettings = endpointConfiguration.UseCommonTransport();
routingSettings.RouteToEndpoint(typeof(FindBestLoan), "LoanBroker");

builder.UseNServiceBus(endpointConfiguration);

var app = builder.Build();

await app.StartAsync();

const ConsoleKey sendMessageConsoleKey = ConsoleKey.F;
const ConsoleKey continuousSendMessageConsoleKey = ConsoleKey.L;
Console.WriteLine($"Press {sendMessageConsoleKey} to send a new FindBestLoan request");
Console.WriteLine($"Press {continuousSendMessageConsoleKey} to send a new FindBestLoan request every second");
Console.WriteLine("Press Q to quit");

var messageSession = app.Services.GetRequiredService<IMessageSession>();
var running = true;
var continuousSend = false;
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    running = false;
};

while (running)
{
    if (Console.KeyAvailable)
    {
        var k = Console.ReadKey(true);
        switch (k.Key)
        {
            case sendMessageConsoleKey:
                await SendMessage(messageSession);
                break;
            case continuousSendMessageConsoleKey:
                continuousSend = true;
                break;
            case ConsoleKey.Q:
                running = false;
                break;
        }
    }

    if (continuousSend)
    {
        await Task.Delay(1000);
        await SendMessage(messageSession);
    }
}

await app.StopAsync();
app.Dispose();
return;

static Task SendMessage(IMessageSession messageSession)
{
    var requestId = Guid.NewGuid().ToString()[..8];
    var prospect = new Prospect("Scrooge", "McDuck");
    Console.WriteLine(
        $"Sending FindBestLoan for prospect {prospect.Name} {prospect.Surname}. Request ID: {requestId}");

    var sendOptions = new SendOptions();
    sendOptions.SetHeader(LoanBrokerHeaders.RequestId, requestId);

    var findBestLoan = new FindBestLoan(requestId, prospect, 10, 1000);

    return messageSession.Send(findBestLoan, sendOptions);
}