// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Pipelines;

namespace EricksonLopez.Resilience.Builder;

/// <summary>
/// Implements a strongly-typed resilience pipeline builder for accumulating, configuring, and compiling typed resilience strategies.
/// </summary>
/// <typeparam name="TResult">The result type returned by resilient operations executed through this pipeline.</typeparam>
public sealed class ResiliencePipelineBuilder<TResult>
{
    private readonly List<ResilienceStrategyOptions> _strategies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePipelineBuilder{TResult}"/> class.
    /// </summary>
    /// <param name="name">The unique logical name of the pipeline.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or whitespace</exception>
    public ResiliencePipelineBuilder(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the unique logical name of the pipeline.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the list of configured resilience strategies in execution order.
    /// </summary>
    public IReadOnlyList<ResilienceStrategyOptions> Strategies => _strategies;

    /// <summary>
    /// Adds a retry resilience strategy to the typed pipeline.
    /// </summary>
    /// <param name="options">The retry strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    /// <exception cref="ResilienceConfigurationException">The retry configuration options are invalid</exception>
    public ResiliencePipelineBuilder<TResult> AddRetry(RetryStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateRetryOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <summary>
    /// Adds a retry resilience strategy configured via an action delegate to the typed pipeline.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/></exception>
    public ResiliencePipelineBuilder<TResult> AddRetry(Action<RetryStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new RetryStrategyOptions();
        configure(options);
        return AddRetry(options);
    }

    /// <summary>
    /// Adds a circuit breaker resilience strategy to the typed pipeline.
    /// </summary>
    /// <param name="options">The circuit breaker strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    /// <exception cref="ResilienceConfigurationException">The circuit breaker configuration options are invalid</exception>
    public ResiliencePipelineBuilder<TResult> AddCircuitBreaker(CircuitBreakerStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateCircuitBreakerOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <summary>
    /// Adds a circuit breaker resilience strategy configured via an action delegate to the typed pipeline.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/></exception>
    public ResiliencePipelineBuilder<TResult> AddCircuitBreaker(Action<CircuitBreakerStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new CircuitBreakerStrategyOptions();
        configure(options);
        return AddCircuitBreaker(options);
    }

    /// <summary>
    /// Adds a timeout resilience strategy to the typed pipeline.
    /// </summary>
    /// <param name="options">The timeout strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    /// <exception cref="ResilienceConfigurationException">The timeout configuration options are invalid</exception>
    public ResiliencePipelineBuilder<TResult> AddTimeout(TimeoutStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateTimeoutOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <summary>
    /// Adds a timeout resilience strategy configured via an action delegate to the typed pipeline.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/></exception>
    public ResiliencePipelineBuilder<TResult> AddTimeout(Action<TimeoutStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new TimeoutStrategyOptions();
        configure(options);
        return AddTimeout(options);
    }

    /// <summary>
    /// Adds a rate limiter resilience strategy to the typed pipeline.
    /// </summary>
    /// <param name="options">The rate limiter strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    /// <exception cref="ResilienceConfigurationException">The rate limiter configuration options are invalid</exception>
    public ResiliencePipelineBuilder<TResult> AddRateLimiter(RateLimiterStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateRateLimiterOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <summary>
    /// Adds a rate limiter resilience strategy configured via an action delegate to the typed pipeline.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/></exception>
    public ResiliencePipelineBuilder<TResult> AddRateLimiter(Action<RateLimiterStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new RateLimiterStrategyOptions();
        configure(options);
        return AddRateLimiter(options);
    }

    /// <summary>
    /// Adds a strongly-typed fallback resilience strategy to the typed pipeline.
    /// </summary>
    /// <param name="options">The fallback strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    /// <exception cref="ResilienceConfigurationException"><see cref="FallbackStrategyOptions{TResult}.FallbackAction"/> is <see langword="null"/></exception>
    public ResiliencePipelineBuilder<TResult> AddFallback(FallbackStrategyOptions<TResult> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.FallbackAction == null)
        {
            throw new ResilienceConfigurationException("FallbackStrategyOptions must specify a valid FallbackAction delegate.");
        }

        _strategies.Add(options);
        return this;
    }

    /// <summary>
    /// Adds a strongly-typed fallback resilience strategy configured via an action delegate to the typed pipeline.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/></exception>
    public ResiliencePipelineBuilder<TResult> AddFallback(Action<FallbackStrategyOptions<TResult>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new FallbackStrategyOptions<TResult>();
        configure(options);
        return AddFallback(options);
    }

    /// <summary>
    /// Adds a strongly-typed parallel speculative hedging resilience strategy to the typed pipeline.
    /// </summary>
    /// <param name="options">The hedging strategy options.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    /// <exception cref="ResilienceConfigurationException">The hedging configuration options are invalid</exception>
    public ResiliencePipelineBuilder<TResult> AddHedging(HedgingStrategyOptions<TResult> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateHedgingOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <summary>
    /// Adds a strongly-typed parallel speculative hedging resilience strategy configured via an action delegate to the typed pipeline.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/></exception>
    public ResiliencePipelineBuilder<TResult> AddHedging(Action<HedgingStrategyOptions<TResult>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new HedgingStrategyOptions<TResult>();
        configure(options);
        return AddHedging(options);
    }

    /// <summary>
    /// Compiles the configured strategies into an executable <see cref="IResiliencePipeline{TResult}"/>.
    /// </summary>
    /// <returns>The compiled typed resilience pipeline.</returns>
    public IResiliencePipeline<TResult> Build()
    {
        return ResiliencePipelineBuilder.CompileTyped(this);
    }

    private static void ValidateRetryOptions(RetryStrategyOptions options)
    {
        if (options.MaxRetryAttempts < 0)
        {
            throw new ResilienceConfigurationException("Retry MaxRetryAttempts must be greater than or equal to 0.");
        }

        if (options.Delay < TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("Retry Delay must be greater than or equal to TimeSpan.Zero.");
        }

        if (options.MaxDelay.HasValue && options.MaxDelay.Value < options.Delay)
        {
            throw new ResilienceConfigurationException("Retry MaxDelay cannot be less than base Delay.");
        }
    }

    private static void ValidateCircuitBreakerOptions(CircuitBreakerStrategyOptions options)
    {
        if (options.FailureRatio is <= 0.0 or > 1.0)
        {
            throw new ResilienceConfigurationException("CircuitBreaker FailureRatio must be greater than 0.0 and less than or equal to 1.0.");
        }

        if (options.MinimumThroughput <= 0)
        {
            throw new ResilienceConfigurationException("CircuitBreaker MinimumThroughput must be greater than 0.");
        }

        if (options.SamplingDuration <= TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("CircuitBreaker SamplingDuration must be greater than TimeSpan.Zero.");
        }

        if (options.BreakDuration <= TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("CircuitBreaker BreakDuration must be greater than TimeSpan.Zero.");
        }
    }

    private static void ValidateTimeoutOptions(TimeoutStrategyOptions options)
    {
        if (options.Timeout <= TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("Timeout must be greater than TimeSpan.Zero.");
        }
    }

    private static void ValidateRateLimiterOptions(RateLimiterStrategyOptions options)
    {
        if (options.CustomRateLimiter == null)
        {
            if (options.PermitLimit <= 0)
            {
                throw new ResilienceConfigurationException("RateLimiter PermitLimit must be greater than 0.");
            }

            if (options.QueueLimit < 0)
            {
                throw new ResilienceConfigurationException("RateLimiter QueueLimit cannot be negative.");
            }

            if (options.Window <= TimeSpan.Zero)
            {
                throw new ResilienceConfigurationException("RateLimiter Window must be greater than TimeSpan.Zero.");
            }
        }
    }

    private static void ValidateHedgingOptions(HedgingStrategyOptions<TResult> options)
    {
        if (options.MaxHedgedAttempts <= 0)
        {
            throw new ResilienceConfigurationException("Hedging MaxHedgedAttempts must be greater than 0.");
        }

        if (options.Delay < TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("Hedging Delay cannot be negative.");
        }
    }
}
