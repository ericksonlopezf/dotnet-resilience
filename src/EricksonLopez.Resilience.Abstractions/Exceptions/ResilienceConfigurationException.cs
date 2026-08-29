// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Exceptions;

/// <summary>
/// Represents the exception thrown when a resilience strategy or pipeline configuration contains invalid options or invariants.
/// </summary>
public sealed class ResilienceConfigurationException : ResilienceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The message describing the invalid configuration.</param>
    public ResilienceConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceConfigurationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message describing the invalid configuration.</param>
    /// <param name="innerException">The inner exception that caused the configuration failure.</param>
    public ResilienceConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
