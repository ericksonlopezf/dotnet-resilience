// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.Options;

namespace EricksonLopez.Resilience.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IResiliencePipelineBuilder"/> to configure common resilience patterns.
/// </summary>
public static class ResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Adds a retry strategy configured to evaluate <see cref="EricksonLopez.Result.Result{T}"/>,
    /// <see cref="EricksonLopez.Result.Result"/>, and domain errors for transient retryability.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">An optional configuration action for fine-tuning retry options.</param>
    /// <param name="classifier">An optional custom classifier instance.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static IResiliencePipelineBuilder AddResultRetry(
        this IResiliencePipelineBuilder builder,
        Action<RetryStrategyOptions>? configure = null,
        IResultRetryClassifier? classifier = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var activeClassifier = classifier ?? ResultRetryClassifier.Instance;
        var options = new RetryStrategyOptions
        {
            ShouldHandleException = ex => activeClassifier.ClassifyException(ex) == RetryabilityDecision.Retry,
            ShouldHandleResult = res => activeClassifier.ClassifyResult(res) == RetryabilityDecision.Retry
        };

        configure?.Invoke(options);
        return builder.AddRetry(options);
    }

    /// <summary>
    /// Adds a retry strategy configured to evaluate <see cref="EricksonLopez.Result.Result{T}"/>,
    /// <see cref="EricksonLopez.Result.Result"/>, and domain errors for transient retryability in a typed pipeline.
    /// </summary>
    /// <typeparam name="TResult">The result type of the pipeline.</typeparam>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">An optional configuration action for fine-tuning retry options.</param>
    /// <param name="classifier">An optional custom classifier instance.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static ResiliencePipelineBuilder<TResult> AddResultRetry<TResult>(
        this ResiliencePipelineBuilder<TResult> builder,
        Action<RetryStrategyOptions>? configure = null,
        IResultRetryClassifier? classifier = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var activeClassifier = classifier ?? ResultRetryClassifier.Instance;
        var options = new RetryStrategyOptions
        {
            ShouldHandleException = ex => activeClassifier.ClassifyException(ex) == RetryabilityDecision.Retry,
            ShouldHandleResult = res => activeClassifier.ClassifyResult(res) == RetryabilityDecision.Retry
        };

        configure?.Invoke(options);
        return builder.AddRetry(options);
    }

    /// <summary>
    /// Adds a standard resilience pipeline preset comprising timeout, retry with exponential jitter, and a circuit breaker.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static IResiliencePipelineBuilder AddStandardResilience(this IResiliencePipelineBuilder builder)
    {
        return builder
            .AddTimeout(TimeSpan.FromSeconds(30))
            .AddResultRetry(opt =>
            {
                opt.MaxRetryAttempts = 3;
                opt.Delay = TimeSpan.FromSeconds(1);
                opt.BackoffType = BackoffType.ExponentialWithJitter;
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions());
    }

    /// <summary>
    /// Adds a standard resilience pipeline preset comprising timeout, retry with exponential jitter, and a circuit breaker to a typed pipeline.
    /// </summary>
    /// <typeparam name="TResult">The result type of the pipeline.</typeparam>
    /// <param name="builder">The pipeline builder.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static ResiliencePipelineBuilder<TResult> AddStandardResilience<TResult>(this ResiliencePipelineBuilder<TResult> builder)
    {
        return builder
            .AddTimeout(TimeSpan.FromSeconds(30))
            .AddResultRetry(opt =>
            {
                opt.MaxRetryAttempts = 3;
                opt.Delay = TimeSpan.FromSeconds(1);
                opt.BackoffType = BackoffType.ExponentialWithJitter;
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions());
    }

    /// <summary>
    /// Adds a database resilience preset optimized for transient connection drops and transient serialization failures.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="timeout">The optional overall execution timeout duration.</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static IResiliencePipelineBuilder AddDatabaseResilience(
        this IResiliencePipelineBuilder builder,
        TimeSpan? timeout = null,
        int maxRetries = 3)
    {
        return builder
            .AddTimeout(timeout ?? TimeSpan.FromSeconds(15))
            .AddResultRetry(opt =>
            {
                opt.MaxRetryAttempts = maxRetries;
                opt.Delay = TimeSpan.FromMilliseconds(200);
                opt.BackoffType = BackoffType.ExponentialWithJitter;
                opt.MaxDelay = TimeSpan.FromSeconds(2);
            });
    }

    /// <summary>
    /// Adds a database resilience preset optimized for transient connection drops and transient serialization failures to a typed pipeline.
    /// </summary>
    /// <typeparam name="TResult">The result type of the pipeline.</typeparam>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="timeout">The optional overall execution timeout duration.</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static ResiliencePipelineBuilder<TResult> AddDatabaseResilience<TResult>(
        this ResiliencePipelineBuilder<TResult> builder,
        TimeSpan? timeout = null,
        int maxRetries = 3)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddTimeout(timeout ?? TimeSpan.FromSeconds(15))
            .AddResultRetry(opt =>
            {
                opt.MaxRetryAttempts = maxRetries;
                opt.Delay = TimeSpan.FromMilliseconds(200);
                opt.BackoffType = BackoffType.ExponentialWithJitter;
                opt.MaxDelay = TimeSpan.FromSeconds(2);
            });
    }

    /// <summary>
    /// Adds a timeout strategy to the pipeline with the specified timeout duration.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static IResiliencePipelineBuilder AddTimeout(
        this IResiliencePipelineBuilder builder,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddTimeout(new TimeoutStrategyOptions { Timeout = timeout });
    }

    /// <summary>
    /// Adds a timeout strategy to the typed pipeline with the specified timeout duration.
    /// </summary>
    /// <typeparam name="TResult">The result type of the pipeline.</typeparam>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static ResiliencePipelineBuilder<TResult> AddTimeout<TResult>(
        this ResiliencePipelineBuilder<TResult> builder,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddTimeout(new TimeoutStrategyOptions { Timeout = timeout });
    }
}
