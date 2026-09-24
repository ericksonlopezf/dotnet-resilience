// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Policies;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EricksonLopez.Resilience.DependencyInjection.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private sealed class FastTimeoutPolicy : ResiliencePolicy
    {
        public override string Name => "fast-timeout";

        public override void Configure(IResiliencePipelineBuilder builder)
        {
            builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromMilliseconds(20));
        }
    }

    private sealed class AnotherFastPolicy : ResiliencePolicy
    {
        public override string Name => "another-fast";

        public override void Configure(IResiliencePipelineBuilder builder)
        {
            builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromMilliseconds(30));
        }
    }

    [Fact]
    public void AddEricksonLopezResilience_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => services!.AddEricksonLopezResilience());

        // Assert
        ex.ParamName.Should().Be("services");
    }

    [Fact]
    public void AddResiliencePolicy_TypedPolicy_RegistersInServiceCollectionDirectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResiliencePolicy<FastTimeoutPolicy>();

        // Assert - verify both AddEricksonLopezResilience and singleton registration
        services.Should().Contain(sd => sd.ServiceType == typeof(IResiliencePolicy) && sd.ImplementationType == typeof(FastTimeoutPolicy));
        services.Should().Contain(sd => sd.ServiceType == typeof(ResiliencePolicyRegistry));

        var sp = services.BuildServiceProvider();
        var policy = sp.GetRequiredService<IResiliencePolicy>();
        policy.Should().BeOfType<FastTimeoutPolicy>();
    }

    [Fact]
    public void AddResiliencePolicy_NamedAction_RegistersEricksonLopezResilienceDirectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResiliencePolicy("direct-named", (Action<IResiliencePipelineBuilder>)(b => { }));

        // Assert - verify both AddEricksonLopezResilience and named policy registration
        services.Should().Contain(sd => sd.ServiceType == typeof(NamedPolicyRegistration));
        services.Should().Contain(sd => sd.ServiceType == typeof(ResiliencePolicyRegistry));

        var sp = services.BuildServiceProvider();
        var registry = sp.GetService<IResiliencePipelineRegistry>();
        registry.Should().NotBeNull();
    }

    [Fact]
    public void AddEricksonLopezResilience_RegistersCoreServicesAndExecutor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddEricksonLopezResilience();

        // Assert
        returnedServices.Should().BeSameAs(services);
        var provider = services.BuildServiceProvider();

        // Singletons
        var policyRegistry = provider.GetService<ResiliencePolicyRegistry>();
        policyRegistry.Should().NotBeNull();

        var pipelineRegistry = provider.GetService<ResiliencePipelineRegistry>();
        pipelineRegistry.Should().NotBeNull();

        var iPipelineRegistry = provider.GetService<IResiliencePipelineRegistry>();
        iPipelineRegistry.Should().BeSameAs(pipelineRegistry);

        var retryClassifier = provider.GetService<ResultRetryClassifier>();
        retryClassifier.Should().NotBeNull();

        var iRetryClassifier = provider.GetService<IResultRetryClassifier>();
        iRetryClassifier.Should().BeSameAs(retryClassifier);

        var iErrorClassifier = provider.GetService<IErrorClassifier>();
        iErrorClassifier.Should().BeSameAs(retryClassifier);

        var executor = provider.GetService<IResilienceExecutor>();
        executor.Should().NotBeNull();
        executor.Should().BeOfType<PollyResilienceExecutor>();
    }

    [Fact]
    public void AddEricksonLopezResilience_WithLogging_InjectsLoggerIntoExecutor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<PollyResilienceExecutor>>(NullLogger<PollyResilienceExecutor>.Instance);
        services.AddEricksonLopezResilience();

        var provider = services.BuildServiceProvider();

        // Act
        var executor = provider.GetRequiredService<IResilienceExecutor>();

        // Assert
        executor.Should().BeOfType<PollyResilienceExecutor>();
    }

    [Fact]
    public async Task AddEricksonLopezResilience_WithOptionsConfigure_RegistersNamedPoliciesAndExecutes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEricksonLopezResilience(options =>
        {
            options.AddPolicy("options-policy", builder =>
            {
                builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromMilliseconds(25));
            });
        });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IResiliencePipelineRegistry>();
        var pipeline = registry.GetPipeline("options-policy");

        // Assert
        pipeline.Should().NotBeNull();
        (pipeline as PollyResiliencePipeline)?.Name.Should().Be("options-policy");

        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult(42));
        result.Should().Be(42);

        // Verify strategy in options-policy actually took effect
        Func<Task> slowAct = async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(150, ct);
                return 42;
            });
        };
        await slowAct.Should().ThrowAsync<ResilienceTimeoutException>();
    }

    [Fact]
    public void AddResiliencePolicy_Typed_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddResiliencePolicy<FastTimeoutPolicy>();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddResiliencePolicy_TypedPolicy_CompilesAndRegistersInRegistry()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddResiliencePolicy<FastTimeoutPolicy>();
        services.AddResiliencePolicy<AnotherFastPolicy>();

        // Assert
        returnedServices.Should().BeSameAs(services);
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IResiliencePipelineRegistry>();
        var policyRegistry = provider.GetRequiredService<ResiliencePolicyRegistry>();

        // Check IResiliencePolicy singleton instances are in DI
        var registeredPolicies = provider.GetServices<IResiliencePolicy>().ToList();
        registeredPolicies.Should().HaveCount(2);
        registeredPolicies.Should().Contain(p => p is FastTimeoutPolicy);
        registeredPolicies.Should().Contain(p => p is AnotherFastPolicy);

        var pipeline = registry.GetPipeline("fast-timeout");
        pipeline.Should().NotBeNull();
        (pipeline as PollyResiliencePipeline)?.Name.Should().Be("fast-timeout");

        var anotherPipeline = registry.GetPipeline("another-fast");
        anotherPipeline.Should().NotBeNull();

        policyRegistry.TryGetPolicy("fast-timeout", out var pol).Should().BeTrue();
        pol.Should().BeOfType<FastTimeoutPolicy>();

        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("Paid"));
        result.Should().Be("Paid");

        // Verify policy.Configure(builder) actually configured the timeout
        Func<Task> slowAct = async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(150, ct);
                return "Slow";
            });
        };
        await slowAct.Should().ThrowAsync<ResilienceTimeoutException>();
    }

    [Fact]
    public void AddResiliencePolicy_Named_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddResiliencePolicy("dynamic-policy", (Action<IResiliencePipelineBuilder>)(b => { }));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddResiliencePolicy_Named_WithNullOrWhitespaceName_ThrowsArgumentException(string? policyName)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddResiliencePolicy(policyName!, (Action<IResiliencePipelineBuilder>)(b => { }));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddResiliencePolicy_Named_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddResiliencePolicy("dynamic-policy", (Action<IResiliencePipelineBuilder>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddResiliencePolicy_NamedAction_CompilesAndRegistersInRegistry()
    {
        // Arrange - Fresh services without explicit AddEricksonLopezResilience
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddResiliencePolicy("dynamic-policy", builder =>
        {
            builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromMilliseconds(20));
        });

        // Assert
        returnedServices.Should().BeSameAs(services);
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IResiliencePipelineRegistry>();

        var pipeline = registry.GetPipeline("dynamic-policy");
        pipeline.Should().NotBeNull();
        (pipeline as PollyResiliencePipeline)?.Name.Should().Be("dynamic-policy");

        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("Done"));
        result.Should().Be("Done");

        // Verify named.Configure(builder) actually configured the timeout
        Func<Task> slowAct = async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(150, ct);
                return "Slow";
            });
        };
        await slowAct.Should().ThrowAsync<ResilienceTimeoutException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddResiliencePolicy_WithServiceProviderAction_WhenPolicyNameIsInvalid_ThrowsArgumentException(string? policyName)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddResiliencePolicy(policyName!, (Action<IResiliencePipelineBuilder, IServiceProvider>)((b, sp) => { }));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddResiliencePolicy_WithServiceProviderAction_WhenConfigureIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddResiliencePolicy("dynamic-policy", (Action<IResiliencePipelineBuilder, IServiceProvider>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddResiliencePolicy_WithServiceProviderAction_CompilesAndRegistersInRegistryWithServiceProviderResolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton("injected-metadata");

        string? capturedValue = null;

        // Act
        var returnedServices = services.AddResiliencePolicy("sp-action-policy", (builder, sp) =>
        {
            capturedValue = sp.GetRequiredService<string>();
            builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromMilliseconds(20));
        });

        // Assert
        returnedServices.Should().BeSameAs(services);
        services.Should().Contain(sd => sd.ServiceType == typeof(NamedPolicyRegistration));
        services.Should().Contain(sd => sd.ServiceType == typeof(ResiliencePolicyRegistry));

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IResiliencePipelineRegistry>();

        var pipeline = registry.GetPipeline("sp-action-policy");
        pipeline.Should().NotBeNull();
        (pipeline as PollyResiliencePipeline)?.Name.Should().Be("sp-action-policy");
        capturedValue.Should().Be("injected-metadata");

        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("Done"));
        result.Should().Be("Done");

        Func<Task> slowAct = async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(150, ct);
                return "Slow";
            });
        };
        await slowAct.Should().ThrowAsync<ResilienceTimeoutException>();
    }
}
