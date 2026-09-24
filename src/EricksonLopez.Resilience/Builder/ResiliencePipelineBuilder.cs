// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Resilience.Options;

namespace EricksonLopez.Resilience.Builder;

/// <summary>
/// Provides a builder implementation for accumulating, configuring, and validating resilience strategies to construct an <see cref="IResiliencePipeline"/>.
/// </summary>
public sealed class ResiliencePipelineBuilder : IResiliencePipelineBuilder
{
    private static volatile Func<IResiliencePipelineBuilder, IResiliencePipeline>? _pipelineFactory;
    private readonly List<ResilienceStrategyOptions> _strategies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePipelineBuilder"/> class.
    /// </summary>
    /// <param name="name">The unique logical name of the pipeline.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or whitespace</exception>
    public ResiliencePipelineBuilder(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IReadOnlyList<ResilienceStrategyOptions> Strategies => _strategies;

    /// <summary>
    /// Registers the global pipeline factory used to compile resilience builders into executable pipelines.
    /// </summary>
    /// <param name="factory">The pipeline compilation factory delegate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/></exception>
    public static void SetPipelineFactory(Func<IResiliencePipelineBuilder, IResiliencePipeline> factory)
    {
        _pipelineFactory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Registers the global typed pipeline factory used to compile typed resilience builders into executable pipelines.
    /// </summary>
    /// <typeparam name="TResult">The result type of the pipeline.</typeparam>
    /// <param name="factory">The typed pipeline compilation factory delegate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/></exception>
    public static void SetTypedPipelineFactory<TResult>(Func<ResiliencePipelineBuilder<TResult>, IResiliencePipeline<TResult>> factory)
    {
        TypedFactoryHolder<TResult>.Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal static IResiliencePipeline<TResult> CompileTyped<TResult>(ResiliencePipelineBuilder<TResult> builder)
    {
        if (TypedFactoryHolder<TResult>.Factory != null)
        {
            return TypedFactoryHolder<TResult>.Factory(builder);
        }

        throw new InvalidOperationException(
            $"Cannot compile typed resilience pipeline '{builder.Name}' for result type '{typeof(TResult).FullName}'. " +
            $"No typed pipeline compilation factory has been registered. Ensure that EricksonLopez.Resilience.Polly is referenced and initialized via " +
            $"PollyResilienceRegistration.RegisterTypedPipeline<{typeof(TResult).Name}>() or dependency injection services.AddResiliencePipeline<{typeof(TResult).Name}>().");
    }

    private static class TypedFactoryHolder<TResult>
    {
        internal static volatile Func<ResiliencePipelineBuilder<TResult>, IResiliencePipeline<TResult>>? Factory;
    }

    /// <inheritdoc/>
    public IResiliencePipelineBuilder AddRetry(RetryStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ResilienceStrategyValidator.ValidateRetryOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <inheritdoc/>
    public IResiliencePipelineBuilder AddRetry(Action<RetryStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new RetryStrategyOptions();
        configure(options);
        return AddRetry(options);
    }

    /// <inheritdoc/>
    public IResiliencePipelineBuilder AddCircuitBreaker(CircuitBreakerStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ResilienceStrategyValidator.ValidateCircuitBreakerOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <inheritdoc/>
    public IResiliencePipelineBuilder AddCircuitBreaker(Action<CircuitBreakerStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new CircuitBreakerStrategyOptions();
        configure(options);
        return AddCircuitBreaker(options);
    }

    /// <inheritdoc/>
    public IResiliencePipelineBuilder AddTimeout(TimeoutStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ResilienceStrategyValidator.ValidateTimeoutOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <inheritdoc/>
    public IResiliencePipelineBuilder AddTimeout(Action<TimeoutStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new TimeoutStrategyOptions();
        configure(options);
        return AddTimeout(options);
    }

    /// <inheritdoc/>
    public IResiliencePipelineBuilder AddRateLimiter(RateLimiterStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ResilienceStrategyValidator.ValidateRateLimiterOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <inheritdoc/>
    public IResiliencePipelineBuilder AddRateLimiter(Action<RateLimiterStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new RateLimiterStrategyOptions();
        configure(options);
        return AddRateLimiter(options);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Untyped hedging cannot perform concurrent speculative executions in Polly v8
    /// and falls back to sequential retry. Use <see cref="ResiliencePipelineBuilder{TResult}.AddHedging(HedgingStrategyOptions{TResult})"/>
    /// for parallel speculative hedging.
    /// </remarks>
    public IResiliencePipelineBuilder AddHedging(HedgingStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ResilienceStrategyValidator.ValidateHedgingOptions(options);
        _strategies.Add(options);
        return this;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Untyped hedging cannot perform concurrent speculative executions in Polly v8
    /// and falls back to sequential retry. Use <see cref="ResiliencePipelineBuilder{TResult}.AddHedging(Action{HedgingStrategyOptions{TResult}})"/>
    /// for parallel speculative hedging.
    /// </remarks>
    public IResiliencePipelineBuilder AddHedging(Action<HedgingStrategyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new HedgingStrategyOptions();
        configure(options);
        return AddHedging(options);
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">No pipeline compilation factory has been registered</exception>
    public IResiliencePipeline Build()
    {
        if (_pipelineFactory != null)
        {
            return _pipelineFactory(this);
        }

        throw new InvalidOperationException(
            $"Cannot compile resilience pipeline '{Name}'. " +
            $"No pipeline compilation factory has been registered. Ensure that EricksonLopez.Resilience.Polly is referenced and initialized via " +
            $"PollyResilienceRegistration.Initialize() or dependency injection services.AddEricksonLopezResilience().");
    }
}
