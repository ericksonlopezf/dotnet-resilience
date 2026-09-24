// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Defines configuration options for the retry resilience strategy.
/// </summary>
public sealed class RetryStrategyOptions : ResilienceStrategyOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryStrategyOptions"/> class with default settings.
    /// </summary>
    public RetryStrategyOptions()
    {
    }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts allowed before failing the execution.
    /// Default is 3 attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay between retry attempts.
    /// Default is 2 seconds.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the backoff algorithm used to compute delay between consecutive retry attempts.
    /// Default is <see cref="BackoffType.ExponentialWithJitter"/>.
    /// </summary>
    public BackoffType BackoffType { get; set; } = BackoffType.ExponentialWithJitter;

    /// <summary>
    /// Gets or sets the maximum allowed delay cap when backoff increases.
    /// If <see langword="null"/>, no upper limit is enforced.
    /// </summary>
    public TimeSpan? MaxDelay { get; set; }

    /// <summary>
    /// Gets or sets an optional predicate to determine whether an observed exception should trigger a retry attempt.
    /// </summary>
    public Func<Exception, bool>? ShouldHandleException { get; set; }

    /// <summary>
    /// Gets or sets an optional predicate to determine whether an observed result value should trigger a retry attempt.
    /// </summary>
    public Func<object?, bool>? ShouldHandleResult { get; set; }

    /// <summary>
    /// Gets or sets an asynchronous callback invoked prior to waiting and executing the next retry attempt.
    /// </summary>
    public Func<RetryAttemptContext, ValueTask>? OnRetry { get; set; }
}
