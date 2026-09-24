// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Resilience.Options;

namespace EricksonLopez.Resilience;

/// <summary>
/// Defines a builder for configuring and assembling resilience strategies into an executable pipeline.
/// </summary>
public interface IResiliencePipelineBuilder
{
    /// <summary>
    /// Gets the unique name of the pipeline being built.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the collection of configured strategy options in order of addition.
    /// </summary>
    IReadOnlyList<ResilienceStrategyOptions> Strategies { get; }

    /// <summary>
    /// Adds a retry strategy to the pipeline.
    /// </summary>
    /// <param name="options">The retry strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    IResiliencePipelineBuilder AddRetry(RetryStrategyOptions options);

    /// <summary>
    /// Adds a retry strategy to the pipeline configured via an action delegate.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    IResiliencePipelineBuilder AddRetry(Action<RetryStrategyOptions> configure);

    /// <summary>
    /// Adds a circuit breaker strategy to the pipeline.
    /// </summary>
    /// <param name="options">The circuit breaker strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    IResiliencePipelineBuilder AddCircuitBreaker(CircuitBreakerStrategyOptions options);

    /// <summary>
    /// Adds a circuit breaker strategy to the pipeline configured via an action delegate.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    IResiliencePipelineBuilder AddCircuitBreaker(Action<CircuitBreakerStrategyOptions> configure);

    /// <summary>
    /// Adds a timeout strategy to the pipeline.
    /// </summary>
    /// <param name="options">The timeout strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    IResiliencePipelineBuilder AddTimeout(TimeoutStrategyOptions options);

    /// <summary>
    /// Adds a timeout strategy to the pipeline configured via an action delegate.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    IResiliencePipelineBuilder AddTimeout(Action<TimeoutStrategyOptions> configure);

    /// <summary>
    /// Adds a rate limiter strategy to the pipeline.
    /// </summary>
    /// <param name="options">The rate limiter strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    IResiliencePipelineBuilder AddRateLimiter(RateLimiterStrategyOptions options);

    /// <summary>
    /// Adds a rate limiter strategy to the pipeline configured via an action delegate.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    IResiliencePipelineBuilder AddRateLimiter(Action<RateLimiterStrategyOptions> configure);

    /// <summary>
    /// Adds a hedging strategy to the pipeline.
    /// </summary>
    /// <param name="options">The hedging strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <remarks>
    /// Untyped hedging cannot perform concurrent speculative executions in Polly v8 and falls back to sequential retry. Use <c>ResiliencePipelineBuilder&lt;TResult&gt;.AddHedging</c> instead for parallel hedging.
    /// </remarks>
    IResiliencePipelineBuilder AddHedging(HedgingStrategyOptions options);

    /// <summary>
    /// Adds a hedging strategy to the pipeline configured via an action delegate.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <remarks>
    /// Untyped hedging cannot perform concurrent speculative executions in Polly v8 and falls back to sequential retry. Use <c>ResiliencePipelineBuilder&lt;TResult&gt;.AddHedging</c> instead for parallel hedging.
    /// </remarks>
    IResiliencePipelineBuilder AddHedging(Action<HedgingStrategyOptions> configure);

    /// <summary>
    /// Builds and returns the compiled <see cref="IResiliencePipeline"/>.
    /// </summary>
    /// <returns>The compiled resilience pipeline.</returns>
    IResiliencePipeline Build();
}
