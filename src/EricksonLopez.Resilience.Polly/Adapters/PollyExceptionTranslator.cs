// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Resilience.Exceptions;
using global::Polly.CircuitBreaker;
using global::Polly.RateLimiting;
using global::Polly.Timeout;

namespace EricksonLopez.Resilience.Polly.Adapters;

internal static class PollyExceptionTranslator
{
    public static Exception Translate(Exception ex, string? policyName)
    {
        var resolvedPolicyName = policyName ?? "Resilience";
        return ex switch
        {
            TimeoutRejectedException timeoutEx => new ResilienceTimeoutException(timeoutEx.Timeout, resolvedPolicyName, timeoutEx),
            IsolatedCircuitException isoEx => new CircuitBrokenException(resolvedPolicyName, null, isoEx),
            BrokenCircuitException brokenEx => new CircuitBrokenException(resolvedPolicyName, brokenEx.RetryAfter, brokenEx),
            RateLimiterRejectedException rateEx => new RateLimitRejectedException(resolvedPolicyName, rateEx.RetryAfter, rateEx),
            _ => ex
        };
    }
}
