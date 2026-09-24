// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Pipelines;

/// <summary>
/// Represents a fallback typed resilience pipeline that executes delegates directly without additional strategy interceptors.
/// </summary>
/// <typeparam name="TResult">The type of value returned by the operation.</typeparam>
public sealed class PassthroughResiliencePipeline<TResult> : IResiliencePipeline<TResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PassthroughResiliencePipeline{TResult}"/> class.
    /// </summary>
    /// <param name="name">The name of the pipeline.</param>
    /// <remarks>
    /// For unit testing, construct a named instance directly: <c>new PassthroughResiliencePipeline&lt;TResult&gt;("test")</c>.
    /// For untyped pipelines, use <see cref="PassthroughResiliencePipeline.Instance"/> instead.
    /// </remarks>
    public PassthroughResiliencePipeline(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the pipeline.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc/>
    public ValueTask<TResult> ExecuteAsync(
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
    public ValueTask<TResult> ExecuteAsync(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        return operation(cancellationToken);
    }
}
