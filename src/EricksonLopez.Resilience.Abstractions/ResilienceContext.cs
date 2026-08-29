// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;

namespace EricksonLopez.Resilience;

/// <summary>
/// Represents the execution context for a resilient operation within the EricksonLopez ecosystem.
/// </summary>
/// <remarks>
/// Encapsulates execution metadata including policy identification, operation naming, correlation tracking,
/// tenant isolation, and attempt tracking without introducing static or mutable ambient state.
/// <para>
/// The read-only properties <see cref="PolicyName"/>, <see cref="OperationName"/>, <see cref="CorrelationId"/>,
/// <see cref="TenantId"/>, and <see cref="CancellationToken"/> are set at construction and are immutable for the lifetime
/// of the context instance. <see cref="SetProperty"/> mutates the internal properties dictionary in-place and is
/// not thread-safe for concurrent callers. <see cref="AttemptNumber"/> is updated by the executor framework on each retry attempt.
/// </para>
/// </remarks>
public sealed class ResilienceContext
{
    private readonly Dictionary<string, object?> _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceContext"/> class.
    /// </summary>
    /// <param name="policyName">The unique identifier of the resilience policy.</param>
    /// <param name="operationName">The optional human-readable name of the executing operation.</param>
    /// <param name="correlationId">The optional correlation identifier for distributed tracing.</param>
    /// <param name="tenantId">The optional tenant identifier for multi-tenant isolation.</param>
    /// <param name="cancellationToken">The cancellation token associated with the execution.</param>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public ResilienceContext(
        string policyName,
        string? operationName = null,
        string? correlationId = null,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        PolicyName = policyName;
        OperationName = operationName ?? policyName;
        CorrelationId = correlationId;
        TenantId = tenantId;
        CancellationToken = cancellationToken;
        _properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the unique name of the resilience policy.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// Gets the human-readable name of the executing operation.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets the correlation identifier for distributed tracing and logging.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the tenant identifier for multi-tenant partition and isolation.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the cancellation token governing this execution.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets or sets the current execution attempt index (1-based).
    /// </summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>
    /// Gets a read-only view of custom context properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties => _properties;

    /// <summary>
    /// Creates a new <see cref="ResilienceContext"/> instance with the specified operation name.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <returns>A new context instance with the updated operation name.</returns>
    public ResilienceContext WithOperationName(string operationName)
    {
        var next = new ResilienceContext(PolicyName, operationName, CorrelationId, TenantId, CancellationToken)
        {
            AttemptNumber = AttemptNumber
        };
        CopyPropertiesTo(next);
        return next;
    }

    /// <summary>
    /// Creates a new <see cref="ResilienceContext"/> instance with the specified correlation identifier.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <returns>A new context instance with the updated correlation identifier.</returns>
    public ResilienceContext WithCorrelationId(string correlationId)
    {
        var next = new ResilienceContext(PolicyName, OperationName, correlationId, TenantId, CancellationToken)
        {
            AttemptNumber = AttemptNumber
        };
        CopyPropertiesTo(next);
        return next;
    }

    /// <summary>
    /// Creates a new <see cref="ResilienceContext"/> instance with the specified tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>A new context instance with the updated tenant identifier.</returns>
    public ResilienceContext WithTenantId(string tenantId)
    {
        var next = new ResilienceContext(PolicyName, OperationName, CorrelationId, tenantId, CancellationToken)
        {
            AttemptNumber = AttemptNumber
        };
        CopyPropertiesTo(next);
        return next;
    }

    /// <summary>
    /// Creates a new <see cref="ResilienceContext"/> instance with the specified attempt number.
    /// </summary>
    /// <param name="attemptNumber">The attempt number.</param>
    /// <returns>A new context instance with the updated attempt number.</returns>
    public ResilienceContext WithAttemptNumber(int attemptNumber)
    {
        var next = new ResilienceContext(PolicyName, OperationName, CorrelationId, TenantId, CancellationToken)
        {
            AttemptNumber = attemptNumber
        };
        CopyPropertiesTo(next);
        return next;
    }

    /// <summary>
    /// Sets a custom property value in the execution context.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <returns>The current context instance for method chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or whitespace</exception>
    public ResilienceContext SetProperty(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _properties[key] = value;
        return this;
    }

    /// <summary>
    /// Attempts to retrieve a strongly-typed property value from the execution context.
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="value">When this method returns, contains the property value if found and of type <typeparamref name="T"/>; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the property exists and matches the specified type; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or whitespace</exception>
    public bool TryGetProperty<T>(string key, out T? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_properties.TryGetValue(key, out var raw) && raw is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Creates a default empty context for a given policy name and cancellation token.
    /// </summary>
    /// <param name="policyName">The name of the resilience policy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new <see cref="ResilienceContext"/> instance.</returns>
    public static ResilienceContext Create(string policyName, CancellationToken cancellationToken = default)
    {
        return new ResilienceContext(policyName, cancellationToken: cancellationToken);
    }

    private void CopyPropertiesTo(ResilienceContext destination)
    {
        foreach (var (k, v) in _properties)
        {
            destination._properties[k] = v;
        }
    }
}
