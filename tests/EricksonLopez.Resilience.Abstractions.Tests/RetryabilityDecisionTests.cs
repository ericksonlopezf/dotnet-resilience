// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.Resilience.Classification;
using Xunit;

namespace EricksonLopez.Resilience.Abstractions.Tests;

public sealed class RetryabilityDecisionTests
{
    [Fact]
    public void RetryabilityDecision_HasExpectedValues()
    {
        ((byte)RetryabilityDecision.Undetermined).Should().Be(0);
        ((byte)RetryabilityDecision.Retry).Should().Be(1);
        ((byte)RetryabilityDecision.DoNotRetry).Should().Be(2);
    }
}
