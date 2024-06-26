using ClientMessages;
using Microsoft.Extensions.Logging;

namespace Client.Handlers;

public class BestLoanFoundHandler(ILogger<BestLoanFoundHandler> logger) : IHandleMessages<BestLoanFound>
{
    public Task Handle(BestLoanFound message, IMessageHandlerContext context)
    {
        logger.LogInformation("The best loan rate for request id {RequestId} is: {InterestRate} offered by {BankId}", message.RequestId, message.InterestRate, message.BankId);
        return Task.CompletedTask;
    }
}