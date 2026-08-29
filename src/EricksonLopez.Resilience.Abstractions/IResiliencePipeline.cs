// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience;

/// <summary>
/// Defines a resilience execution pipeline capable of executing asynchronous delegates with fault-tolerance strategies.
/// </summary>
public interface IResiliencePipeline
{
    /// <summary>
    /// Executes an asynchronous operation that returns a value within this resilience pipeline using the specified context.
    /// </summary>
    /// <typeparam name="TResult">The return type of the operation.</typeparam>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <param name="context">The resilience context carrying execution metadata and cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the value produced by <paramref name="operation"/>.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<ResilienceContext, ValueTask<TResult>> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous operation that returns a value within this resilience pipeline.
    /// </summary>
    /// <typeparam name="TResult">The return type of the operation.</typeparam>
    /// <param name="operation">The asynchronous operation accepting a cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the value produced by <paramref name="operation"/>.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous action without a return value within this resilience pipeline using the specified context.
    /// </summary>
    /// <param name="operation">The asynchronous action to execute.</param>
    /// <param name="context">The resilience context carrying execution metadata and cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(
        Func<ResilienceContext, ValueTask> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous action without a return value within this resilience pipeline.
    /// </summary>
    /// <param name="operation">The asynchronous action accepting a cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default);
}
