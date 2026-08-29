# Testing Strategies & Verification

## Overview

Testing resilient code can be challenging due to time delays, randomized jitter, and concurrency. `EricksonLopez.Resilience` is designed from the ground up for deterministic, ultra-fast unit and integration testing.

## Unit Testing with Passthrough Pipelines

When testing domain services or application handlers, you often want to bypass delays and retry loops. Use `PassthroughResiliencePipeline`:

```csharp
using EricksonLopez.Resilience.Pipelines;
using EricksonLopez.Resilience.Registry;

[Fact]
public async Task ApplicationService_WhenTestedInIsolation_UsesPassthroughPipeline()
{
    // Arrange
    var registry = new ResiliencePipelineRegistry();
    registry.Register("order-policy", new PassthroughResiliencePipeline("order-policy"));

    var executor = new PollyResilienceExecutor(registry);
    var service = new OrderService(executor, mockRepo);

    // Act
    var result = await service.CreateOrderAsync(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

---

## Testing Retries Deterministically

For tests verifying that an operation retries 3 times and succeeds on the 3rd attempt, configure a pipeline with zero delay:

```csharp
var builder = new ResiliencePipelineBuilder("test-retry");
builder.AddRetry(opt =>
{
    opt.MaxRetryAttempts = 3;
    opt.Delay = TimeSpan.Zero; // Instant execution in tests
    opt.ShouldHandleException = ex => ex is IOException;
});

var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

var attempts = 0;
var result = await pipeline.ExecuteAsync<string>(async ct =>
{
    attempts++;
    if (attempts < 3) throw new IOException("Transient drop");
    return "Success";
});

attempts.Should().Be(3);
result.Should().Be("Success");
```
