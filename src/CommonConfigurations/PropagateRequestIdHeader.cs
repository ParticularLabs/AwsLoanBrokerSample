using NServiceBus.Features;
using NServiceBus.Pipeline;

namespace CommonConfigurations;

public class PropagateRequestIdHeader : Feature
{
    public PropagateRequestIdHeader()
    {
        EnableByDefault();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Pipeline.Register(new GetRequestIdBehavior(), "Retrieve the RequestID from the incoming message");
        context.Pipeline.Register(new SetRequestIdBehavior(), "Set the RequestID in the headers of outgoing messages");
    }
}

class GetRequestIdBehavior : Behavior<IIncomingLogicalMessageContext>
{
    public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        if (context.MessageHeaders.TryGetRequestId(out var requestId))
        {
            context.Extensions.Set(LoanBrokerHeaders.RequestId, requestId);
        }
        return next();
    }
}

class SetRequestIdBehavior : Behavior<IOutgoingLogicalMessageContext>
{
    public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
    {
        if (context.Extensions.TryGet(LoanBrokerHeaders.RequestId, out string requestId))
        {
            context.Headers.Add(LoanBrokerHeaders.RequestId, requestId);
        }

        return next();
    }
}