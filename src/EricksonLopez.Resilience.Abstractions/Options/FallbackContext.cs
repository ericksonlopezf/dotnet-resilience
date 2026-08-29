// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Provides contextual information when a fallback strategy is evaluated or executed.
/// </summary>
public sealed class FallbackContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackContext"/> class.
    /// </summary>
    /// <param name="context">The ambient resilience context.</param>
    /// <param name="exception">The exception that triggered the fallback, if any.</param>
    /// <param name="result">The outcome result that triggered the fallback, if any.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/></exception>
    public FallbackContext(
        ResilienceContext context,
        Exception? exception = null,
        object? result = null)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Exception = exception;
        Result = result;
    }

    /// <summary>
    /// Gets the ambient resilience context.
    /// </summary>
    public ResilienceContext Context { get; }

    /// <summary>
    /// Gets the exception that caused the operation failure, or <see langword="null"/> if the failure was outcome-based.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the outcome result that triggered the fallback, or <see langword="null"/> if the failure was exception-based.
    /// </summary>
    public object? Result { get; }
}
