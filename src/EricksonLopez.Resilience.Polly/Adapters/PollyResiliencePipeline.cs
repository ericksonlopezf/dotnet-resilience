// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using global::Polly.CircuitBreaker;
using global::Polly.RateLimiting;
using global::Polly.Timeout;

namespace EricksonLopez.Resilience.Polly.Adapters;

/// <summary>
/// Provides an <see cref="IResiliencePipeline"/> implementation backed by a compiled Polly v8 <see cref="global::Polly.ResiliencePipeline"/>.
/// </summary>
public sealed class PollyResiliencePipeline : IResiliencePipeline
{
    private readonly global::Polly.ResiliencePipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyResiliencePipeline"/> class.
    /// </summary>
    /// <param name="name">The unique name of the pipeline.</param>
    /// <param name="pipeline">The compiled Polly resilience pipeline.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> is <see langword="null"/></exception>
    public PollyResiliencePipeline(string name, global::Polly.ResiliencePipeline pipeline)
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
    public async ValueTask<TResult> ExecuteAsync<TResult>(
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
            effectiveToken)
        {
            AttemptNumber = context.AttemptNumber
        };
        context.CopyPropertiesTo(initialContext);

        var pollyContext = PollyContextAdapter.ToPollyContext(initialContext);
        try
        {
            var stateTuple = (initialContext, context, operation);
            return await _pipeline.ExecuteAsync(
                new Func<global::Polly.ResilienceContext, (ResilienceContext initialContext, ResilienceContext context, Func<ResilienceContext, ValueTask<TResult>> operation), ValueTask<TResult>>(
                async static (pCtx, state) =>
                {
                    var ecoCtx = PollyContextAdapter.GetEcosystemContext(pCtx) ?? state.initialContext;
                    var executionContext = new ResilienceContext(
                        state.context.PolicyName,
                        state.context.OperationName,
                        state.context.CorrelationId,
                        state.context.TenantId,
                        pCtx.CancellationToken)
                    {
                        AttemptNumber = ecoCtx.AttemptNumber
                    };
                    ecoCtx.CopyPropertiesTo(executionContext);

                    var result = await state.operation(executionContext).ConfigureAwait(false);

                    executionContext.CopyPropertiesTo(ecoCtx);
                    executionContext.CopyPropertiesTo(state.context);
                    state.context.AttemptNumber = executionContext.AttemptNumber;
                    return result;
                }),
                pollyContext,
                stateTuple).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutRejectedException or IsolatedCircuitException or BrokenCircuitException or RateLimiterRejectedException)
        {
            throw PollyExceptionTranslator.Translate(ex, context.PolicyName);
        }
        finally
        {
            PollyContextAdapter.Return(pollyContext);
        }
    }

    /// <inheritdoc/>
    public ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var context = ResilienceContext.Create(Name, cancellationToken);
        return ExecuteAsync(ctx => operation(ctx.CancellationToken), context, cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(
        Func<ResilienceContext, ValueTask> operation,
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
            effectiveToken)
        {
            AttemptNumber = context.AttemptNumber
        };
        context.CopyPropertiesTo(initialContext);

        var pollyContext = PollyContextAdapter.ToPollyContext(initialContext);
        try
        {
            var stateTuple = (initialContext, context, operation);
            await _pipeline.ExecuteAsync(
                new Func<global::Polly.ResilienceContext, (ResilienceContext initialContext, ResilienceContext context, Func<ResilienceContext, ValueTask> operation), ValueTask>(
                async static (pCtx, state) =>
                {
                    var ecoCtx = PollyContextAdapter.GetEcosystemContext(pCtx) ?? state.initialContext;
                    var executionContext = new ResilienceContext(
                        state.context.PolicyName,
                        state.context.OperationName,
                        state.context.CorrelationId,
                        state.context.TenantId,
                        pCtx.CancellationToken)
                    {
                        AttemptNumber = ecoCtx.AttemptNumber
                    };
                    ecoCtx.CopyPropertiesTo(executionContext);

                    await state.operation(executionContext).ConfigureAwait(false);

                    executionContext.CopyPropertiesTo(ecoCtx);
                    executionContext.CopyPropertiesTo(state.context);
                    state.context.AttemptNumber = executionContext.AttemptNumber;
                }),
                pollyContext,
                stateTuple).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutRejectedException or IsolatedCircuitException or BrokenCircuitException or RateLimiterRejectedException)
        {
            throw PollyExceptionTranslator.Translate(ex, context.PolicyName);
        }
        finally
        {
            PollyContextAdapter.Return(pollyContext);
        }
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var context = ResilienceContext.Create(Name, cancellationToken);
        return ExecuteAsync(ctx => operation(ctx.CancellationToken), context, cancellationToken);
    }
}
