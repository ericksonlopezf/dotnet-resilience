// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Exceptions;

/// <summary>
/// Represents the base exception type for all resilience-related errors in the EricksonLopez ecosystem.
/// </summary>
public class ResilienceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceException"/> class with a default error message.
    /// </summary>
    public ResilienceException()
        : base("A resilience fault occurred during operation execution.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ResilienceException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner exception that is the cause of the current exception.</param>
    public ResilienceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
