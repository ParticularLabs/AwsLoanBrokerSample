using System.Diagnostics;
using System.Diagnostics.Metrics;
using BankMessages;
using ClientMessages;
using CommonConfigurations;
using LoanBroker.Services;
using Microsoft.Extensions.Logging;

namespace LoanBroker.Policies;

class BestLoanPolicy(
    ILogger<BestLoanPolicy> logger,
    ICreditScoreProvider creditScoreProvider,
    IQuoteAggregator quoteAggregator) : Saga<BestLoanData>,
    IAmStartedByMessages<FindBestLoan>,
    IHandleMessages<QuoteCreated>,
    IHandleMessages<QuoteRequestRefusedByBank>,
    IHandleTimeouts<MaxTimeout>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<BestLoanData> mapper)
    {
        mapper.MapSaga(saga => saga.RequestId)
            .ToMessage<FindBestLoan>(message => message.RequestId)
            .ToMessage<QuoteCreated>(message => message.RequestId)
            .ToMessage<QuoteRequestRefusedByBank>(message => message.RequestId);
    }

    public async Task Handle(FindBestLoan message, IMessageHandlerContext context)
    {
        logger.LogInformation($"FindBestLoan request received from {message.Prospect}, with ID {message.RequestId}. Details: number of years {message.NumberOfYears}, amount: {message.Amount}");

        var score = creditScoreProvider.Score(message.Prospect);
        await context.Publish(new QuoteRequested(message.RequestId,
            score,
            message.NumberOfYears,
            message.Amount
        ));
        Data.RequestSentToBanks = DateTime.UtcNow;
        var requestExpiration = TimeSpan.FromSeconds(10);
        await RequestTimeout<MaxTimeout>(context, requestExpiration);
        logger.LogInformation($"Quote, with request ID {message.RequestId}, requested to banks. The request expires in {requestExpiration}");
    }

    public Task Handle(QuoteCreated message, IMessageHandlerContext context)
    {
        logger.LogInformation($"Quote, for request ID {message.RequestId}, received from bank {message.BankId}. Interest rate: {message.InterestRate}");
        Data.Quotes.Add(new Quote(message.BankId, message.InterestRate));

        var tags = new TagList(
        [
            new(Tags.MessageType, typeof(QuoteCreated)),
            new(Tags.BankId,message.BankId),
        ]);

        if (Data.RequestSentToBanks.HasValue)
        {
            BankResponseTime.Record((DateTime.UtcNow - Data.RequestSentToBanks!.Value).TotalMilliseconds, tags);
        }
        return Task.CompletedTask;
    }

    public Task Handle(QuoteRequestRefusedByBank message, IMessageHandlerContext context)
    {
        logger.LogWarning($"Quote, for request ID {message.RequestId}, refused by bank {message.BankId}. Request: {message.RequestId}");
        Data.RejectedBy.Add(message.BankId);

        var tags = new TagList(
        [
            new(Tags.MessageType, typeof(QuoteRequestRefusedByBank)),
            new(Tags.BankId,message.BankId),
        ]);

        if (Data.RequestSentToBanks.HasValue)
        {
            BankResponseTime.Record((DateTime.UtcNow - Data.RequestSentToBanks!.Value).TotalMilliseconds, tags);
        }
        return Task.CompletedTask;
    }

    public async Task Timeout(MaxTimeout timeout, IMessageHandlerContext context)
    {
        IMessage replyMessage;

        if (Data.Quotes.Count > 0)
        {
            var quote = quoteAggregator.Reduce(Data.Quotes);
            replyMessage = new BestLoanFound(Data.RequestId, quote.BankId, quote.InterestRate);
            logger.LogInformation($"Best Loan found for request ID {Data.RequestId}, from bank {quote.BankId}. Details, interest rate: {quote.InterestRate}");
        }
        else if (Data.RejectedBy.Count > 0)
        {
            replyMessage = new QuoteRequestRefused(Data.RequestId);
            logger.LogWarning($"All banks that responded rejected the quote request with ID {Data.RequestId}");
        }
        else
        {
            replyMessage = new NoQuotesReceived(Data.RequestId);
            logger.LogWarning($"The request ID {Data.RequestId} expired with no responses from banks");
        }

        await ReplyToOriginator(context, replyMessage);
        MarkAsComplete();
    }

    static readonly Histogram<double> BankResponseTime =
        SharedConventions.LoanBrokerMeter.CreateHistogram<double>("loan_broker.bank_processing_time", "s",
            "The time banks take to respond to quote requests.");
}

class BestLoanData : ContainSagaData
{
    public DateTime? RequestSentToBanks { get; set; } = null;
    public string RequestId { get; set; } = null!;
    public List<Quote> Quotes { get; set; } = [];
    public List<string> RejectedBy { get; set; } = [];
}

record MaxTimeout;

static class Tags
{
    public const string MessageType = "nservicebus.message_type";
    public const string BankId = "loan_broker.bank_id";
}
