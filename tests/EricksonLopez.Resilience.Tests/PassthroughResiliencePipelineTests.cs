// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.Pipelines;
using Xunit;

namespace EricksonLopez.Resilience.Tests;

public sealed class PassthroughResiliencePipelineTests
{
    [Fact]
    public async Task ExecuteAsync_GenericWithContext_ExecutesAndPropagatesContext()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("passthrough-policy");
        var context = ResilienceContext.Create("passthrough-policy").SetProperty("key", "val");

        // Act
        var result = await pipeline.ExecuteAsync(async ctx =>
        {
            await Task.Yield();
            ctx.TryGetProperty<string>("key", out var v);
            return $"retrieved-{v}";
        }, context);

        // Assert
        result.Should().Be("retrieved-val");
        pipeline.Name.Should().Be("passthrough-policy");
    }

    [Fact]
    public async Task ExecuteAsync_GenericWithContext_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("passthrough-policy");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = ResilienceContext.Create("passthrough-policy");

        // Act & Assert
        var act1 = async () => await pipeline.ExecuteAsync(async _ => { await Task.Yield(); return 1; }, context, cts.Token);
        await act1.Should().ThrowAsync<OperationCanceledException>();

        var act2 = async () => await pipeline.ExecuteAsync(async _ => { await Task.Yield(); return 1; }, new ResilienceContext("pol", cancellationToken: cts.Token));
        await act2.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_GenericWithContext_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("pol");
        var context = ResilienceContext.Create("pol");

        // Act & Assert
        var act1 = async () => await pipeline.ExecuteAsync<int>(null!, context);
        var act2 = async () => await pipeline.ExecuteAsync(async _ => { await Task.Yield(); return 1; }, null!);

        await act1.Should().ThrowAsync<ArgumentNullException>();
        await act2.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_GenericWithToken_PassesDirectlyThrough()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("passthrough-policy");

        // Act
        var result = await pipeline.ExecuteAsync(async ct =>
        {
            await Task.Yield();
            return 100;
        });

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public async Task ExecuteAsync_GenericWithToken_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("pol");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await pipeline.ExecuteAsync(async _ => { await Task.Yield(); return 10; }, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_GenericWithToken_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("pol");

        // Act & Assert
        var act = async () => await pipeline.ExecuteAsync<string>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_VoidWithContext_ExecutesSuccessfully()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("passthrough-void-ctx");
        var context = ResilienceContext.Create("passthrough-void-ctx");
        var executed = false;

        // Act
        await pipeline.ExecuteAsync(async ctx =>
        {
            await Task.Yield();
            executed = true;
        }, context);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_VoidWithContext_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("passthrough-void-ctx");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = ResilienceContext.Create("passthrough-void-ctx");

        // Act & Assert
        var act1 = async () => await pipeline.ExecuteAsync(async _ => await Task.Yield(), context, cts.Token);
        await act1.Should().ThrowAsync<OperationCanceledException>();

        var act2 = async () => await pipeline.ExecuteAsync(async _ => await Task.Yield(), new ResilienceContext("pol", cancellationToken: cts.Token));
        await act2.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_VoidWithContext_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("pol");
        var context = ResilienceContext.Create("pol");

        // Act & Assert
        var act1 = async () => await pipeline.ExecuteAsync(null!, context);
        var act2 = async () => await pipeline.ExecuteAsync(async _ => await Task.Yield(), null!);

        await act1.Should().ThrowAsync<ArgumentNullException>();
        await act2.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_VoidWithToken_PassesDirectlyThrough()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("passthrough-void");
        var executed = false;

        // Act
        await pipeline.ExecuteAsync(async ct =>
        {
            await Task.Yield();
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_VoidWithToken_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("pol");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await pipeline.ExecuteAsync(async _ => await Task.Yield(), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_VoidWithToken_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline("pol");

        // Act & Assert
        var act = async () => await pipeline.ExecuteAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TypedPassthroughPipeline_ExecutesWithContextAndToken()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline<string>("typed-passthrough");
        var context = ResilienceContext.Create("typed-passthrough");

        // Act
        var res1 = await pipeline.ExecuteAsync(async ctx =>
        {
            await Task.Yield();
            return "typed-context-res";
        }, context);

        var res2 = await pipeline.ExecuteAsync(async ct =>
        {
            await Task.Yield();
            return "typed-token-res";
        });

        // Assert
        res1.Should().Be("typed-context-res");
        res2.Should().Be("typed-token-res");
        pipeline.Name.Should().Be("typed-passthrough");
    }

    [Fact]
    public async Task TypedPassthroughPipeline_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline<string>("typed-passthrough");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = ResilienceContext.Create("typed-passthrough");

        // Act & Assert
        var act1 = async () => await pipeline.ExecuteAsync(async _ => { await Task.Yield(); return "ok"; }, context, cts.Token);
        var act2 = async () => await pipeline.ExecuteAsync(async _ => { await Task.Yield(); return "ok"; }, new ResilienceContext("pol", cancellationToken: cts.Token));
        var act3 = async () => await pipeline.ExecuteAsync(async _ => { await Task.Yield(); return "ok"; }, cts.Token);

        await act1.Should().ThrowAsync<OperationCanceledException>();
        await act2.Should().ThrowAsync<OperationCanceledException>();
        await act3.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TypedPassthroughPipeline_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = new PassthroughResiliencePipeline<int>("typed-passthrough");
        var context = ResilienceContext.Create("typed-passthrough");

        // Act & Assert
        var act1 = async () => await pipeline.ExecuteAsync(null!, context);
        var act2 = async () => await pipeline.ExecuteAsync(async _ => { await Task.Yield(); return 1; }, null!);
        var act3 = async () => await pipeline.ExecuteAsync(null!);

        await act1.Should().ThrowAsync<ArgumentNullException>();
        await act2.Should().ThrowAsync<ArgumentNullException>();
        await act3.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Instance_HasCorrectDefaultName()
    {
        PassthroughResiliencePipeline.Instance.Name.Should().Be("passthrough");
    }

    [Fact]
    public async Task ExecuteAsync_UntypedGeneric_EffectiveContextResolution_MatchesExpectedTokens()
    {
        var pipeline = new PassthroughResiliencePipeline("passthrough");
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        var contextWithToken = new ResilienceContext("pol", cancellationToken: cts1.Token);

        // Case 1: default token -> receives original context
        ResilienceContext? received1 = null;
        await pipeline.ExecuteAsync(ctx => { received1 = ctx; return ValueTask.FromResult(1); }, contextWithToken, default);
        received1.Should().BeSameAs(contextWithToken);

        // Case 2: same token -> receives original context
        ResilienceContext? received2 = null;
        await pipeline.ExecuteAsync(ctx => { received2 = ctx; return ValueTask.FromResult(2); }, contextWithToken, cts1.Token);
        received2.Should().BeSameAs(contextWithToken);

        // Case 3: different token -> receives new context with token
        ResilienceContext? received3 = null;
        await pipeline.ExecuteAsync(ctx => { received3 = ctx; return ValueTask.FromResult(3); }, contextWithToken, cts2.Token);
        received3.Should().NotBeSameAs(contextWithToken);
        received3!.CancellationToken.Should().Be(cts2.Token);
    }

    [Fact]
    public async Task ExecuteAsync_UntypedNonGeneric_EffectiveContextResolution_MatchesExpectedTokens()
    {
        var pipeline = new PassthroughResiliencePipeline("passthrough");
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        var contextWithToken = new ResilienceContext("pol", cancellationToken: cts1.Token);

        // Case 1: default token -> receives original context
        ResilienceContext? received1 = null;
        await pipeline.ExecuteAsync(ctx => { received1 = ctx; return ValueTask.CompletedTask; }, contextWithToken, default);
        received1.Should().BeSameAs(contextWithToken);

        // Case 2: same token -> receives original context
        ResilienceContext? received2 = null;
        await pipeline.ExecuteAsync(ctx => { received2 = ctx; return ValueTask.CompletedTask; }, contextWithToken, cts1.Token);
        received2.Should().BeSameAs(contextWithToken);

        // Case 3: different token -> receives new context with token
        ResilienceContext? received3 = null;
        await pipeline.ExecuteAsync(ctx => { received3 = ctx; return ValueTask.CompletedTask; }, contextWithToken, cts2.Token);
        received3.Should().NotBeSameAs(contextWithToken);
        received3!.CancellationToken.Should().Be(cts2.Token);
    }

    [Fact]
    public async Task ExecuteAsync_Typed_EffectiveContextResolution_MatchesExpectedTokens()
    {
        var pipeline = new PassthroughResiliencePipeline<string>("passthrough");
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        var contextWithToken = new ResilienceContext("pol", cancellationToken: cts1.Token);

        // Case 1: default token -> receives original context
        ResilienceContext? received1 = null;
        await pipeline.ExecuteAsync(ctx => { received1 = ctx; return ValueTask.FromResult("a"); }, contextWithToken, default);
        received1.Should().BeSameAs(contextWithToken);

        // Case 2: same token -> receives original context
        ResilienceContext? received2 = null;
        await pipeline.ExecuteAsync(ctx => { received2 = ctx; return ValueTask.FromResult("b"); }, contextWithToken, cts1.Token);
        received2.Should().BeSameAs(contextWithToken);

        // Case 3: different token -> receives new context with token
        ResilienceContext? received3 = null;
        await pipeline.ExecuteAsync(ctx => { received3 = ctx; return ValueTask.FromResult("c"); }, contextWithToken, cts2.Token);
        received3.Should().NotBeSameAs(contextWithToken);
        received3!.CancellationToken.Should().Be(cts2.Token);
    }
}
