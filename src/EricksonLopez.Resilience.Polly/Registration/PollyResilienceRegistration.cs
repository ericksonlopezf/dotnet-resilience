// Copyright © Erickson Lopez. MIT License.
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Builders;

namespace EricksonLopez.Resilience.Polly.Registration;

/// <summary>
/// Provides methods for registering the Polly execution engine as the active pipeline compilation provider for <see cref="ResiliencePipelineBuilder"/>.
/// </summary>
public static class PollyResilienceRegistration
{
    private static bool _initialized;
    private static readonly object _syncLock = new();

    internal static object SyncLock => _syncLock;

    /// <summary>
    /// Initializes and configures Polly as the default resilience execution adapter for the ecosystem.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (_syncLock)
        {
            if (_initialized)
            {
                return;
            }

            ResiliencePipelineBuilder.SetPipelineFactory(PollyPipelineBuilderTranslator.TranslateAndBuild);
            _initialized = true;
        }
    }

    /// <summary>
    /// Registers Polly as the compiler for typed pipelines returning <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type of the pipeline.</typeparam>
    public static void RegisterTypedPipeline<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>()
    {
        ResiliencePipelineBuilder.SetTypedPipelineFactory<TResult>(PollyPipelineBuilderTranslator.TranslateAndBuild);
    }
}
