// Copyright © Erickson Lopez. MIT License.
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Registration;
using Xunit;

namespace EricksonLopez.Resilience.Polly.Tests;

public sealed class PollyResilienceRegistrationTests
{
    [Fact]
    public void Initialize_RegistersFactoryAndIsIdempotent()
    {
        var field = typeof(PollyResilienceRegistration).GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Static)!;
        var syncLockField = typeof(PollyResilienceRegistration).GetField("SyncLock", BindingFlags.NonPublic | BindingFlags.Static)!;
        var syncLock = syncLockField.GetValue(null)!;

        field.SetValue(null, false);

        // Act
        PollyResilienceRegistration.Initialize();

        // Verify _initialized is true (kills boolean mutation on line 34)
        var isInit = (bool)field.GetValue(null)!;
        isInit.Should().BeTrue();

        // When _initialized is true, calling Initialize while holding syncLock does not deadlock (kills outer check line 23 return mutation)
        lock (syncLock)
        {
            PollyResilienceRegistration.Initialize();
        }

        // When _initialized is true, calling Initialize does not overwrite custom factory (kills return mutations on line 23 and 30)
        IResiliencePipeline dummyCustom = new PollyResiliencePipeline("custom", new global::Polly.ResiliencePipelineBuilder().Build());
        ResiliencePipelineBuilder.SetPipelineFactory(b => dummyCustom);

        PollyResilienceRegistration.Initialize();
        var customBuilder = new ResiliencePipelineBuilder("custom-test");
        customBuilder.Build().Should().BeSameAs(dummyCustom);

        // Restore factory
        field.SetValue(null, false);
        PollyResilienceRegistration.Initialize();

        // Assert
        var builder = new ResiliencePipelineBuilder("test-polly-reg");
        var pipeline = builder.Build();

        pipeline.Should().NotBeNull();
        var pollyPipeline = pipeline.Should().BeOfType<PollyResiliencePipeline>().Which;
        pollyPipeline.Name.Should().Be("test-polly-reg");
    }

    [Fact]
    public void Initialize_WhenAlreadyInitialized_ReturnsImmediatelyWithoutWaitingForLock()
    {
        var field = typeof(PollyResilienceRegistration).GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Static)!;
        var syncLockField = typeof(PollyResilienceRegistration).GetField("SyncLock", BindingFlags.NonPublic | BindingFlags.Static)!;
        var syncLock = syncLockField.GetValue(null)!;

        field.SetValue(null, true);

        var lockAcquired = new ManualResetEventSlim(false);
        var releaseLock = new ManualResetEventSlim(false);

        var t = new Thread(() =>
        {
            lock (syncLock)
            {
                lockAcquired.Set();
                releaseLock.Wait(2000);
            }
        });
        t.Start();
        lockAcquired.Wait(1000);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        PollyResilienceRegistration.Initialize();
        sw.Stop();

        releaseLock.Set();
        t.Join(1000);

        sw.ElapsedMilliseconds.Should().BeLessThan(500);

        // Reset
        field.SetValue(null, false);
        PollyResilienceRegistration.Initialize();
    }

    [Fact]
    public void Initialize_WhenAlreadyInitializedInsideLock_HitsInnerCheck()
    {
        var field = typeof(PollyResilienceRegistration).GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Static)!;
        var syncLockField = typeof(PollyResilienceRegistration).GetField("SyncLock", BindingFlags.NonPublic | BindingFlags.Static)!;
        var syncLock = syncLockField.GetValue(null)!;

        field.SetValue(null, false);

        IResiliencePipeline dummyCustom = new PollyResiliencePipeline("inner-dummy", new global::Polly.ResiliencePipelineBuilder().Build());

        var lockAcquired = new ManualResetEventSlim(false);
        var proceed = new ManualResetEventSlim(false);

        var t1 = new Thread(() =>
        {
            lock (syncLock)
            {
                lockAcquired.Set();
                proceed.Wait(1000);
                field.SetValue(null, true);
                ResiliencePipelineBuilder.SetPipelineFactory(b => dummyCustom);
            }
        });

        var t2 = new Thread(() =>
        {
            lockAcquired.Wait(1000);
            PollyResilienceRegistration.Initialize();
        });

        t1.Start();
        t2.Start();

        Thread.Sleep(50);
        proceed.Set();

        t1.Join(1000);
        t2.Join(1000);

        // Assert dummy custom factory was not overwritten by t2
        var builder = new ResiliencePipelineBuilder("inner-hit-check");
        var res = builder.Build();
        res.Should().BeSameAs(dummyCustom);

        // Restore factory for any subsequent pipeline building
        field.SetValue(null, false);
        PollyResilienceRegistration.Initialize();

        var normalBuilder = new ResiliencePipelineBuilder("lock-hit");
        normalBuilder.Build().Should().NotBeNull();
    }

    [Fact]
    public void RegisterTypedPipeline_RegistersTypedFactoryAndBuildsTypedPipeline()
    {
        // Act
        PollyResilienceRegistration.RegisterTypedPipeline<string>();

        // Assert
        var builder = new ResiliencePipelineBuilder<string>("test-polly-typed-reg");
        var pipeline = builder.Build();

        pipeline.Should().NotBeNull();
        var pollyPipeline = pipeline.Should().BeOfType<PollyResiliencePipeline<string>>().Which;
        pollyPipeline.Name.Should().Be("test-polly-typed-reg");
    }
}
