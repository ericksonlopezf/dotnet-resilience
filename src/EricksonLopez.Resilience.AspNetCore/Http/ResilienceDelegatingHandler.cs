// Copyright © Erickson Lopez. MIT License.
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.AspNetCore.Http;

/// <summary>
/// Represents a <see cref="DelegatingHandler"/> that executes outgoing HTTP requests within a named <see cref="IResilienceExecutor"/> policy.
/// </summary>
public sealed class ResilienceDelegatingHandler : DelegatingHandler
{
    private readonly IResilienceExecutor _executor;
    private readonly string _policyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceDelegatingHandler"/> class.
    /// </summary>
    /// <param name="executor">The resilience executor.</param>
    /// <param name="policyName">The name of the resilience policy to apply.</param>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public ResilienceDelegatingHandler(IResilienceExecutor executor, string policyName)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        _policyName = policyName;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Content != null)
        {
#if NET9_0_OR_GREATER
            await request.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
#else
            await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);
#endif
        }

        var context = new ResilienceContext(
            _policyName,
            operationName: $"HTTP {request.Method} {request.RequestUri?.Host}",
            cancellationToken: cancellationToken);

        return await _executor.ExecuteAsync(
            _policyName,
            async ctx => await base.SendAsync(request, ctx.CancellationToken).ConfigureAwait(false),
            context,
            cancellationToken).ConfigureAwait(false);
    }
}
