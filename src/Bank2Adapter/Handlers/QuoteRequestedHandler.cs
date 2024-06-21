using BankMessages;
using Microsoft.Extensions.Logging;

namespace Bank2Adapter.Handlers;

public class QuoteRequestedHandler(ILogger<QuoteRequestedHandler> logger) : IHandleMessages<QuoteRequested>
{
    static readonly Random Random = new();
    const string BankIdentifier = "Bank2";

    public async Task Handle(QuoteRequested message, IMessageHandlerContext context)
    {
        logger.LogInformation($"Quote request with ID {message.RequestId}. Details: number of years {message.NumberOfYears}, amount: {message.Amount}, credit score: {message.Score}");

        while (DateTime.Now.Ticks % 300 == 0)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            throw new Exception("Random error");
        }

        while (DateTime.Now.Ticks % 5 == 0)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var randomDelayMilliseconds = Random.Next(500, 3000);
            Thread.Sleep(randomDelayMilliseconds);
        }

        if (Random.Next(0, 5) == 0 || message.Score < 90)
        {
            var quoteRejected = new QuoteRequestRefusedByBank(message.RequestId, BankIdentifier);
            logger.LogWarning($"Quote for request ID {message.RequestId} refused.");

            await context.Reply(quoteRejected);
        }
        else
        {
            var interestRate = Random.NextDouble();
            var quoteCreated = new QuoteCreated(message.RequestId, BankIdentifier, interestRate);
            logger.LogInformation($"Quote created for request ID {message.RequestId}. Details: interest rate: {interestRate}");

            await context.Reply(quoteCreated);
        }
    }
}