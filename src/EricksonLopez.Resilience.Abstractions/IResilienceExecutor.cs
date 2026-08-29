// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience;

/// <summary>
/// Defines the primary resilience execution facade for the EricksonLopez ecosystem.
/// </summary>
/// <remarks>
/// Application services and handlers inject this abstraction to execute operations under declared, named resilience policies
/// without depending directly on concrete execution engines or infrastructure implementations.
/// </remarks>
public interface IResilienceExecutor
{
    /// <summary>
    /// Executes an asynchronous operation that yields a value under the specified resilience policy using an explicit context.
    /// </summary>
    /// <typeparam name="TResult">The return type of the operation.</typeparam>
    /// <param name="policyName">The name of the registered resilience policy to apply.</param>
    /// <param name="operation">The asynchronous operation accepting the execution context.</param>
    /// <param name="context">The resilience context containing metadata and telemetry context.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the value produced by <paramref name="operation"/>.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        string policyName,
        Func<ResilienceContext, ValueTask<TResult>> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous operation that yields a value under the specified resilience policy.
    /// </summary>
    /// <typeparam name="TResult">The return type of the operation.</typeparam>
    /// <param name="policyName">The name of the registered resilience policy to apply.</param>
    /// <param name="operation">The asynchronous operation accepting a cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the value produced by <paramref name="operation"/>.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        string policyName,
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous action without a return value under the specified resilience policy using an explicit context.
    /// </summary>
    /// <param name="policyName">The name of the registered resilience policy to apply.</param>
    /// <param name="operation">The asynchronous action accepting the execution context.</param>
    /// <param name="context">The resilience context containing metadata and telemetry context.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(
        string policyName,
        Func<ResilienceContext, ValueTask> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous action without a return value under the specified resilience policy.
    /// </summary>
    /// <param name="policyName">The name of the registered resilience policy to apply.</param>
    /// <param name="operation">The asynchronous action accepting a cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(
        string policyName,
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default);
}
