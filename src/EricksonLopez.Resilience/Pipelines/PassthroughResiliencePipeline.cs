// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Pipelines;

/// <summary>
/// Represents a fallback resilience pipeline that executes delegates directly without additional strategy interceptors.
/// </summary>
public sealed class PassthroughResiliencePipeline : IResiliencePipeline
{
    /// <summary>
    /// Gets a shared singleton <see cref="PassthroughResiliencePipeline"/> instance with name <c>"passthrough"</c>.
    /// </summary>
    /// <remarks>
    /// Use this instance in unit test suites to bypass all resilience strategy evaluation and execute delegates directly,
    /// enabling deterministic testing without retry, circuit breaker, or timeout interference.
    /// </remarks>
    public static PassthroughResiliencePipeline Instance { get; } = new("passthrough");

    /// <summary>
    /// Initializes a new instance of the <see cref="PassthroughResiliencePipeline"/> class.
    /// </summary>
    /// <param name="name">The name of the pipeline.</param>
    public PassthroughResiliencePipeline(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the pipeline.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc/>
    public ValueTask<TResult> ExecuteAsync<TResult>(
        Func<ResilienceContext, ValueTask<TResult>> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var token = cancellationToken != default ? cancellationToken : context.CancellationToken;
        token.ThrowIfCancellationRequested();
        var effectiveContext = cancellationToken != default && cancellationToken != context.CancellationToken
            ? context.WithCancellationToken(cancellationToken)
            : context;
        return operation(effectiveContext);
    }

    /// <inheritdoc/>
    public ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        return operation(cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(
        Func<ResilienceContext, ValueTask> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var token = cancellationToken != default ? cancellationToken : context.CancellationToken;
        token.ThrowIfCancellationRequested();
        var effectiveContext = cancellationToken != default && cancellationToken != context.CancellationToken
            ? context.WithCancellationToken(cancellationToken)
            : context;
        return operation(effectiveContext);
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        return operation(cancellationToken);
    }
}
