// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.Mediator.Contracts;

namespace EricksonLopez.Resilience.Mediator.Behaviors;

/// <summary>
/// Provides an open generic pipeline behavior that executes mediator requests declaring resilience requirements through <see cref="IResilienceExecutor"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response expected from the pipeline.</typeparam>
public sealed class ResiliencePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IResilienceExecutor _resilienceExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="resilienceExecutor">The resilience executor instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resilienceExecutor"/> is <see langword="null"/></exception>
    public ResiliencePipelineBehavior(IResilienceExecutor resilienceExecutor)
    {
        _resilienceExecutor = resilienceExecutor ?? throw new ArgumentNullException(nameof(resilienceExecutor));
    }

    /// <inheritdoc/>
    public async ValueTask<TResponse> Handle<TNext>(
        TRequest request,
        TNext next,
        CancellationToken cancellationToken)
        where TNext : struct, INext<TResponse>
    {
        if (request is IResilientRequest resilientRequest)
        {
            var policyName = resilientRequest.ResiliencePolicy;
            if (!string.IsNullOrWhiteSpace(policyName))
            {
                var context = new ResilienceContext(
                    policyName,
                    operationName: typeof(TRequest).Name,
                    cancellationToken: cancellationToken);

                return await _resilienceExecutor.ExecuteAsync(
                    policyName,
                    async ctx => await next.InvokeAsync().ConfigureAwait(false),
                    context,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return await next.InvokeAsync().ConfigureAwait(false);
    }
}
