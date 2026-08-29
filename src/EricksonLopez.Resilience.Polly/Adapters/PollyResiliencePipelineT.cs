// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Exceptions;
using global::Polly;
using global::Polly.CircuitBreaker;
using global::Polly.RateLimiting;
using global::Polly.Timeout;

namespace EricksonLopez.Resilience.Polly.Adapters;

/// <summary>
/// Implements <see cref="IResiliencePipeline{TResult}"/> backed by a compiled Polly v8 <see cref="global::Polly.ResiliencePipeline{TResult}"/>.
/// </summary>
/// <typeparam name="TResult">The result type of operations executed by this pipeline.</typeparam>
public sealed class PollyResiliencePipeline<TResult> : IResiliencePipeline<TResult>
{
    private readonly global::Polly.ResiliencePipeline<TResult> _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyResiliencePipeline{TResult}"/> class.
    /// </summary>
    /// <param name="name">The unique name of the pipeline.</param>
    /// <param name="pipeline">The compiled Polly typed resilience pipeline.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> is <see langword="null"/></exception>
    public PollyResiliencePipeline(string name, global::Polly.ResiliencePipeline<TResult> pipeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <summary>
    /// Gets the unique logical name of the resilience pipeline.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc/>
    public async ValueTask<TResult> ExecuteAsync(
        Func<ResilienceContext, ValueTask<TResult>> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var effectiveToken = cancellationToken != default ? cancellationToken : context.CancellationToken;
        var initialContext = new ResilienceContext(
            context.PolicyName,
            context.OperationName,
            context.CorrelationId,
            context.TenantId,
            effectiveToken);

        var pollyContext = PollyContextAdapter.ToPollyContext(initialContext);
        try
        {
            return await _pipeline.ExecuteAsync(
                async (pCtx, state) =>
                {
                    var executionContext = new ResilienceContext(
                        context.PolicyName,
                        context.OperationName,
                        context.CorrelationId,
                        context.TenantId,
                        pCtx.CancellationToken);
                    return await state(executionContext).ConfigureAwait(false);
                },
                pollyContext,
                operation).ConfigureAwait(false);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new ResilienceTimeoutException(ex.Timeout, context.PolicyName, ex);
        }
        catch (IsolatedCircuitException ex)
        {
            throw new CircuitBrokenException(context.PolicyName, null, ex);
        }
        catch (BrokenCircuitException ex)
        {
            throw new CircuitBrokenException(context.PolicyName, ex.RetryAfter, ex);
        }
        catch (RateLimiterRejectedException ex)
        {
            throw new RateLimitRejectedException(context.PolicyName, ex.RetryAfter, ex);
        }
        finally
        {
            PollyContextAdapter.Return(pollyContext);
        }
    }

    /// <inheritdoc/>
    public ValueTask<TResult> ExecuteAsync(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var context = ResilienceContext.Create(Name, cancellationToken);
        return ExecuteAsync(ctx => operation(ctx.CancellationToken), context, cancellationToken);
    }
}
