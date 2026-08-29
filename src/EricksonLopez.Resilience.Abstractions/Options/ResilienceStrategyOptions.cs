// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Represents the abstract base class for resilience strategy configuration options.
/// </summary>
public abstract class ResilienceStrategyOptions
{
    /// <summary>
    /// Gets or sets the optional custom identifier for this strategy instance within a pipeline.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the execution order priority of the strategy within a multi-strategy pipeline (lower numbers execute first).
    /// </summary>
    public int Order { get; set; }
}
