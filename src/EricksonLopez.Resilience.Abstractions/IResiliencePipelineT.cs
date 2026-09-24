// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience;

/// <summary>
/// Defines a strongly-typed resilience execution pipeline capable of executing asynchronous delegates returning <typeparamref name="TResult"/> with fault-tolerance strategies.
/// </summary>
/// <typeparam name="TResult">The type of value returned by operations executed in this pipeline.</typeparam>
public interface IResiliencePipeline<TResult>
{
    /// <summary>
    /// Executes a typed asynchronous operation within this resilience pipeline using the specified context.
    /// </summary>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <param name="context">The resilience context carrying execution metadata and cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the value produced by <paramref name="operation"/>.</returns>
    /// <exception cref="Exceptions.CircuitBrokenException">The circuit breaker rejected the operation because the circuit is open.</exception>
    /// <exception cref="Exceptions.ResilienceTimeoutException">The operation timed out according to the configured timeout limit.</exception>
    /// <exception cref="Exceptions.RateLimitRejectedException">The rate limiter rejected the operation due to limit exhaustion.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    ValueTask<TResult> ExecuteAsync(
        Func<ResilienceContext, ValueTask<TResult>> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a typed asynchronous operation within this resilience pipeline.
    /// </summary>
    /// <param name="operation">The asynchronous operation accepting a cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the value produced by <paramref name="operation"/>.</returns>
    /// <exception cref="Exceptions.CircuitBrokenException">The circuit breaker rejected the operation because the circuit is open.</exception>
    /// <exception cref="Exceptions.ResilienceTimeoutException">The operation timed out according to the configured timeout limit.</exception>
    /// <exception cref="Exceptions.RateLimitRejectedException">The rate limiter rejected the operation due to limit exhaustion.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    ValueTask<TResult> ExecuteAsync(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);
}
