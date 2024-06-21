using BankMessages;
using Microsoft.Extensions.Logging;

namespace Bank3Adapter.Handlers;

public class QuoteRequestedHandler(ILogger<QuoteRequestedHandler> logger) : IHandleMessages<QuoteRequested>
{
    static readonly Random Random = new();
    const string BankIdentifier = "Bank3";

    public async Task Handle(QuoteRequested message, IMessageHandlerContext context)
    {
        logger.LogInformation(
            $"Quote request with ID {message.RequestId}. Details: number of years {message.NumberOfYears}, amount: {message.Amount}, credit score: {message.Score}");

        while (Random.Next(0, 3) == 0)
        {
            throw new Exception("Random error");
        }

        if (Random.Next(0, 2) == 0)
        {
            var quoteRejected = new QuoteRequestRefusedByBank(message.RequestId, BankIdentifier);
            logger.LogWarning($"Quote for request ID {message.RequestId} refused.");

            await context.Reply(quoteRejected);
        }
        else
        {
            var interestRate = Random.NextDouble();
            var quoteCreated = new QuoteCreated(message.RequestId, BankIdentifier, interestRate);
            logger.LogInformation(
                $"Quote created for request ID {message.RequestId}. Details: interest rate: {interestRate}");

            await context.Reply(quoteCreated);
        }
    }
}