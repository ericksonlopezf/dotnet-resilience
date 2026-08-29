// Copyright © Erickson Lopez. MIT License.
using System.Diagnostics.CodeAnalysis;

namespace EricksonLopez.Resilience;

/// <summary>
/// Defines a registry for storing and retrieving configured resilience pipelines by their unique names.
/// </summary>
public interface IResiliencePipelineRegistry
{
    /// <summary>
    /// Retrieves a compiled resilience pipeline associated with the specified policy name.
    /// </summary>
    /// <param name="policyName">The unique name of the policy.</param>
    /// <returns>The resolved <see cref="IResiliencePipeline"/> instance.</returns>
    /// <exception cref="Exceptions.ResiliencePolicyNotFoundException">No pipeline is registered under the specified policy name</exception>
    IResiliencePipeline GetPipeline(string policyName);

    /// <summary>
    /// Attempts to retrieve a compiled resilience pipeline associated with the specified policy name.
    /// </summary>
    /// <param name="policyName">The unique name of the policy.</param>
    /// <param name="pipeline">When this method returns, contains the pipeline if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the pipeline exists; otherwise, <see langword="false"/>.</returns>
    bool TryGetPipeline(string policyName, [NotNullWhen(true)] out IResiliencePipeline? pipeline);

    /// <summary>
    /// Retrieves a typed resilience pipeline associated with the specified policy name.
    /// </summary>
    /// <typeparam name="TResult">The expected result type of the pipeline.</typeparam>
    /// <param name="policyName">The unique name of the policy.</param>
    /// <returns>The resolved <see cref="IResiliencePipeline{TResult}"/> instance.</returns>
    /// <exception cref="Exceptions.ResiliencePolicyNotFoundException">No typed pipeline is registered under the specified policy name</exception>
    IResiliencePipeline<TResult> GetPipeline<TResult>(string policyName) =>
        TryGetPipeline<TResult>(policyName, out var pipeline)
            ? pipeline
            : throw new Exceptions.ResiliencePolicyNotFoundException(policyName);

    /// <summary>
    /// Attempts to retrieve a typed resilience pipeline associated with the specified policy name.
    /// </summary>
    /// <typeparam name="TResult">The expected result type of the pipeline.</typeparam>
    /// <param name="policyName">The unique name of the policy.</param>
    /// <param name="pipeline">When this method returns, contains the typed pipeline if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the typed pipeline exists; otherwise, <see langword="false"/>.</returns>
    bool TryGetPipeline<TResult>(string policyName, [NotNullWhen(true)] out IResiliencePipeline<TResult>? pipeline)
    {
        pipeline = null;
        return false;
    }
}
