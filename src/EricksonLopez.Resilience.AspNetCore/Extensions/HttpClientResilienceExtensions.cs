// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Resilience.AspNetCore.Http;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for configuring resilience policies on named and typed <see cref="System.Net.Http.HttpClient"/> instances.
/// </summary>
public static class HttpClientResilienceExtensions
{
    /// <summary>
    /// Adds a <see cref="ResilienceDelegatingHandler"/> configured with the specified policy name to the HTTP client pipeline.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="policyName">The name of the registered resilience policy to apply.</param>
    /// <returns>The HTTP client builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public static IHttpClientBuilder AddResiliencePolicy(
        this IHttpClientBuilder builder,
        string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        return builder.AddHttpMessageHandler(sp =>
        {
            var executor = sp.GetRequiredService<IResilienceExecutor>();
            return new ResilienceDelegatingHandler(executor, policyName);
        });
    }

    /// <summary>
    /// Adds a standard comprehensive resilience pipeline to the HTTP client comprising total timeout, retry with exponential jitter, circuit breaker, and rate limiting.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="policyName">The optional custom policy name. If omitted, defaults to a name derived from the client.</param>
    /// <param name="configure">An optional delegate to customize the standard HTTP resilience pipeline.</param>
    /// <returns>The HTTP client builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static IHttpClientBuilder AddStandardResilienceHandler(
        this IHttpClientBuilder builder,
        string? policyName = null,
        Action<IResiliencePipelineBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var effectivePolicyName = string.IsNullOrWhiteSpace(policyName)
            ? $"http-standard-{builder.Name}"
            : policyName;

        builder.Services.AddResiliencePolicy(effectivePolicyName, pipelineBuilder =>
        {
            pipelineBuilder
                .AddTimeout(TimeSpan.FromSeconds(30))
                .AddResultRetry(opt =>
                {
                    opt.MaxRetryAttempts = 3;
                    opt.Delay = TimeSpan.FromMilliseconds(500);
                    opt.BackoffType = Options.BackoffType.ExponentialWithJitter;
                    opt.MaxDelay = TimeSpan.FromSeconds(5);
                })
                .AddCircuitBreaker(opt =>
                {
                    opt.FailureRatio = 0.5;
                    opt.MinimumThroughput = 10;
                    opt.SamplingDuration = TimeSpan.FromSeconds(30);
                    opt.BreakDuration = TimeSpan.FromSeconds(15);
                })
                .AddRateLimiter(opt =>
                {
                    opt.PermitLimit = 1000;
                    opt.QueueLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                });

            configure?.Invoke(pipelineBuilder);
        });

        return builder.AddResiliencePolicy(effectivePolicyName);
    }
}
