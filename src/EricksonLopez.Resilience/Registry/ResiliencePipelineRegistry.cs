// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Resilience.Exceptions;

namespace EricksonLopez.Resilience.Registry;

/// <summary>
/// Represents a thread-safe in-memory registry holding compiled <see cref="IResiliencePipeline"/> instances indexed by policy name.
/// </summary>
public sealed class ResiliencePipelineRegistry : IResiliencePipelineRegistry
{
    private readonly ConcurrentDictionary<string, IResiliencePipeline> _pipelines = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, object> _typedPipelines = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePipelineRegistry"/> class.
    /// </summary>
    public ResiliencePipelineRegistry()
    {
    }

    /// <summary>
    /// Registers a compiled resilience pipeline under the specified policy name.
    /// </summary>
    /// <param name="policyName">The unique policy name.</param>
    /// <param name="pipeline">The compiled resilience pipeline instance.</param>
    /// <returns>The registry instance for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> is <see langword="null"/></exception>
    public ResiliencePipelineRegistry Register(string policyName, IResiliencePipeline pipeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(pipeline);

        _pipelines[policyName] = pipeline;
        return this;
    }

    /// <summary>
    /// Registers a compiled typed resilience pipeline under the specified policy name.
    /// </summary>
    /// <typeparam name="TResult">The result type of the pipeline.</typeparam>
    /// <param name="policyName">The unique policy name.</param>
    /// <param name="pipeline">The compiled typed resilience pipeline instance.</param>
    /// <returns>The registry instance for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> is <see langword="null"/></exception>
    public ResiliencePipelineRegistry Register<TResult>(string policyName, IResiliencePipeline<TResult> pipeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(pipeline);

        _typedPipelines[policyName] = pipeline;
        return this;
    }

    /// <inheritdoc/>
    public IResiliencePipeline GetPipeline(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        if (_pipelines.TryGetValue(policyName, out var pipeline))
        {
            return pipeline;
        }

        throw new ResiliencePolicyNotFoundException(policyName);
    }

    /// <inheritdoc/>
    public IResiliencePipeline<TResult> GetPipeline<TResult>(string policyName)
    {
        if (TryGetPipeline<TResult>(policyName, out var pipeline))
        {
            return pipeline;
        }

        throw new ResiliencePolicyNotFoundException(policyName);
    }

    /// <inheritdoc/>
    public bool TryGetPipeline(string policyName, [NotNullWhen(true)] out IResiliencePipeline? pipeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        return _pipelines.TryGetValue(policyName, out pipeline);
    }

    /// <inheritdoc/>
    public bool TryGetPipeline<TResult>(string policyName, [NotNullWhen(true)] out IResiliencePipeline<TResult>? pipeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        if (_typedPipelines.TryGetValue(policyName, out var obj) && obj is IResiliencePipeline<TResult> typedPipeline)
        {
            pipeline = typedPipeline;
            return true;
        }

        pipeline = null;
        return false;
    }
}
