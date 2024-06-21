namespace CommonConfigurations;

using System;
using System.Collections.Generic;
using NServiceBus;

static class Extensions
{
    public static bool TryGetRequestId(this IReadOnlyDictionary<string, string> headers, out string? requestId)
    {
        if (headers.TryGetValue(LoanBrokerHeaders.RequestId, out var requestIdValue))
        {
            requestId = requestIdValue;
            return true;
        }
        requestId = null;
        return false;
    }
}