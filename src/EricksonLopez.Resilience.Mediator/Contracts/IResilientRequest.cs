// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Mediator.Contracts;

/// <summary>
/// Defines a contract for mediator requests that declare an explicit named resilience policy for execution.
/// </summary>
/// <remarks>
/// By implementing this interface, the mediator pipeline behavior automatically intercepts and dispatches
/// the request execution through the corresponding resilience policy without reflection.
/// </remarks>
public interface IResilientRequest
{
    /// <summary>
    /// Gets the unique name of the resilience policy to apply during request execution.
    /// </summary>
    string ResiliencePolicy { get; }
}
