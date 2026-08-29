// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.Polly.Adapters;
using EcoBuilder = EricksonLopez.Resilience.Builder;
using EcoOptions = EricksonLopez.Resilience.Options;
using global::Polly;

namespace EricksonLopez.Resilience.Polly.Builders;

/// <summary>
/// Provides methods to translate ecosystem resilience strategies into Polly v8 <see cref="global::Polly.ResiliencePipeline"/> instances.
/// </summary>
public static class PollyPipelineBuilderTranslator
{
    /// <summary>
    /// Translates an ecosystem <see cref="IResiliencePipelineBuilder"/> into a compiled <see cref="IResiliencePipeline"/> backed by Polly.
    /// </summary>
    /// <param name="builder">The ecosystem builder containing configured strategies.</param>
    /// <returns>A new <see cref="PollyResiliencePipeline"/> wrapping the Polly pipeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static IResiliencePipeline TranslateAndBuild(IResiliencePipelineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder();

        foreach (var strategy in builder.Strategies)
        {
            switch (strategy)
            {
                case EcoOptions.TimeoutStrategyOptions timeoutOpt:
                    AddTimeout(pollyBuilder, timeoutOpt);
                    break;

                case EcoOptions.RetryStrategyOptions retryOpt:
                    AddRetry(pollyBuilder, retryOpt);
                    break;

                case EcoOptions.CircuitBreakerStrategyOptions cbOpt:
                    AddCircuitBreaker(pollyBuilder, cbOpt);
                    break;

                case EcoOptions.RateLimiterStrategyOptions rateOpt:
                    AddRateLimiter(pollyBuilder, rateOpt);
                    break;

                case EcoOptions.HedgingStrategyOptions hedgeOpt:
                    // Hedging in non-generic pipelines is represented as speculative retry
                    AddHedgingFallback(pollyBuilder, hedgeOpt);
                    break;
            }
        }

        var compiledPipeline = pollyBuilder.Build();
        return new PollyResiliencePipeline(builder.Name, compiledPipeline);
    }

    /// <summary>
    /// Translates a strongly-typed <see cref="EcoBuilder.ResiliencePipelineBuilder{TResult}"/> into a compiled <see cref="IResiliencePipeline{TResult}"/> backed by Polly.
    /// </summary>
    /// <typeparam name="TResult">The result type of the pipeline.</typeparam>
    /// <param name="builder">The typed ecosystem builder containing configured strategies.</param>
    /// <returns>A new <see cref="PollyResiliencePipeline{TResult}"/> wrapping the Polly typed pipeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static IResiliencePipeline<TResult> TranslateAndBuild<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(EcoBuilder.ResiliencePipelineBuilder<TResult> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder<TResult>();

        foreach (var strategy in builder.Strategies)
        {
            switch (strategy)
            {
                case EcoOptions.TimeoutStrategyOptions timeoutOpt:
                    AddTimeoutTyped(pollyBuilder, timeoutOpt);
                    break;

                case EcoOptions.RetryStrategyOptions retryOpt:
                    AddRetryTyped(pollyBuilder, retryOpt);
                    break;

                case EcoOptions.CircuitBreakerStrategyOptions cbOpt:
                    AddCircuitBreakerTyped(pollyBuilder, cbOpt);
                    break;

                case EcoOptions.RateLimiterStrategyOptions rateOpt:
                    AddRateLimiterTyped(pollyBuilder, rateOpt);
                    break;

                case EcoOptions.FallbackStrategyOptions<TResult> fallbackOpt:
                    AddFallbackTyped(pollyBuilder, fallbackOpt);
                    break;

                case EcoOptions.HedgingStrategyOptions<TResult> hedgeOpt:
                    AddHedgingParallel(pollyBuilder, hedgeOpt);
                    break;

                case EcoOptions.HedgingStrategyOptions untypedHedgeOpt:
                    AddHedgingFallbackTyped(pollyBuilder, untypedHedgeOpt);
                    break;
            }
        }

        var compiledPipeline = pollyBuilder.Build();
        return new PollyResiliencePipeline<TResult>(builder.Name, compiledPipeline);
    }

    private static void AddTimeout(global::Polly.ResiliencePipelineBuilder builder, EcoOptions.TimeoutStrategyOptions options) =>
        builder.AddTimeout(CreateTimeoutOptions(options));

    private static void AddTimeoutTyped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(global::Polly.ResiliencePipelineBuilder<TResult> builder, EcoOptions.TimeoutStrategyOptions options) =>
        builder.AddTimeout(CreateTimeoutOptions(options));

    private static global::Polly.Timeout.TimeoutStrategyOptions CreateTimeoutOptions(EcoOptions.TimeoutStrategyOptions options)
    {
        var pollyOptions = new global::Polly.Timeout.TimeoutStrategyOptions
        {
            Timeout = options.Timeout,
            Name = options.Name
        };

        if (options.OnTimeout != null)
        {
            pollyOptions.OnTimeout = async args =>
            {
                var ecoCtx = PollyContextAdapter.GetEcosystemContext(args.Context)
                    ?? ResilienceContext.Create(options.Name ?? "Timeout", args.Context.CancellationToken);
                var timeoutContext = new EcoOptions.TimeoutContext(ecoCtx, options.Timeout);
                await options.OnTimeout(timeoutContext).ConfigureAwait(false);
            };
        }

        return pollyOptions;
    }

    private static void AddRetry(global::Polly.ResiliencePipelineBuilder builder, EcoOptions.RetryStrategyOptions options)
    {
        var predicateBuilder = new PredicateBuilder().Handle<Exception>(ex =>
            options.ShouldHandleException?.Invoke(ex) ?? TransientExceptionClassifier.IsTransient(ex));

        if (options.ShouldHandleResult != null)
        {
            predicateBuilder.HandleResult(res => options.ShouldHandleResult(res));
        }

        var pollyOptions = new global::Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            Delay = options.Delay,
            MaxDelay = options.MaxDelay,
            BackoffType = MapBackoffType(options.BackoffType),
            UseJitter = options.BackoffType == EcoOptions.BackoffType.ExponentialWithJitter,
            Name = options.Name,
            ShouldHandle = predicateBuilder
        };

        if (options.OnRetry != null)
        {
            pollyOptions.OnRetry = args => HandleRetry(options, args.Context, args.AttemptNumber, args.RetryDelay, args.Outcome.Exception, args.Outcome.Result);
        }

        builder.AddRetry(pollyOptions);
    }

    private static void AddRetryTyped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(global::Polly.ResiliencePipelineBuilder<TResult> builder, EcoOptions.RetryStrategyOptions options)
    {
        var predicateBuilder = new PredicateBuilder<TResult>().Handle<Exception>(ex =>
            options.ShouldHandleException?.Invoke(ex) ?? TransientExceptionClassifier.IsTransient(ex));

        if (options.ShouldHandleResult != null)
        {
            predicateBuilder.HandleResult(res => options.ShouldHandleResult(res));
        }

        var pollyOptions = new global::Polly.Retry.RetryStrategyOptions<TResult>
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            Delay = options.Delay,
            MaxDelay = options.MaxDelay,
            BackoffType = MapBackoffType(options.BackoffType),
            UseJitter = options.BackoffType == EcoOptions.BackoffType.ExponentialWithJitter,
            Name = options.Name,
            ShouldHandle = predicateBuilder
        };

        if (options.OnRetry != null)
        {
            pollyOptions.OnRetry = args => HandleRetry(options, args.Context, args.AttemptNumber, args.RetryDelay, args.Outcome.Exception, args.Outcome.Result);
        }

        builder.AddRetry(pollyOptions);
    }

    private static async ValueTask HandleRetry(EcoOptions.RetryStrategyOptions options, global::Polly.ResilienceContext context, int attemptNumber, TimeSpan retryDelay, Exception? exception, object? result)
    {
        if (options.OnRetry != null)
        {
            var ecoCtx = PollyContextAdapter.GetEcosystemContext(context)
                ?? ResilienceContext.Create(options.Name ?? "Retry", context.CancellationToken);
            ecoCtx.AttemptNumber = attemptNumber + 1;
            var retryContext = new EcoOptions.RetryAttemptContext(
                ecoCtx,
                attemptNumber + 1,
                retryDelay,
                exception,
                result);
            await options.OnRetry(retryContext).ConfigureAwait(false);
        }
    }

    private static DelayBackoffType MapBackoffType(EcoOptions.BackoffType backoffType) =>
        backoffType switch
        {
            EcoOptions.BackoffType.Constant => DelayBackoffType.Constant,
            EcoOptions.BackoffType.Linear => DelayBackoffType.Linear,
            _ => DelayBackoffType.Exponential
        };

    private static void AddCircuitBreaker(global::Polly.ResiliencePipelineBuilder builder, EcoOptions.CircuitBreakerStrategyOptions options)
    {
        var predicateBuilder = new PredicateBuilder().Handle<Exception>(ex =>
            options.ShouldHandleException?.Invoke(ex) ?? TransientExceptionClassifier.IsTransient(ex));

        if (options.ShouldHandleResult != null)
        {
            predicateBuilder.HandleResult(res => options.ShouldHandleResult(res));
        }

        var pollyOptions = new global::Polly.CircuitBreaker.CircuitBreakerStrategyOptions
        {
            FailureRatio = options.FailureRatio,
            MinimumThroughput = options.MinimumThroughput,
            SamplingDuration = options.SamplingDuration,
            BreakDuration = options.BreakDuration,
            Name = options.Name,
            ShouldHandle = predicateBuilder
        };

        if (options.OnCircuitOpened != null)
        {
            pollyOptions.OnOpened = args => HandleCircuitOpened(options, args.Context, args.BreakDuration, args.Outcome.Exception);
        }
        if (options.OnCircuitClosed != null)
        {
            pollyOptions.OnClosed = args => HandleCircuitClosed(options, args.Context, args.Outcome.Exception);
        }
        if (options.OnCircuitHalfOpened != null)
        {
            pollyOptions.OnHalfOpened = args => HandleCircuitHalfOpened(options, args.Context);
        }

        builder.AddCircuitBreaker(pollyOptions);
    }

    private static void AddCircuitBreakerTyped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(global::Polly.ResiliencePipelineBuilder<TResult> builder, EcoOptions.CircuitBreakerStrategyOptions options)
    {
        var predicateBuilder = new PredicateBuilder<TResult>().Handle<Exception>(ex =>
            options.ShouldHandleException?.Invoke(ex) ?? TransientExceptionClassifier.IsTransient(ex));

        if (options.ShouldHandleResult != null)
        {
            predicateBuilder.HandleResult(res => options.ShouldHandleResult(res));
        }

        var pollyOptions = new global::Polly.CircuitBreaker.CircuitBreakerStrategyOptions<TResult>
        {
            FailureRatio = options.FailureRatio,
            MinimumThroughput = options.MinimumThroughput,
            SamplingDuration = options.SamplingDuration,
            BreakDuration = options.BreakDuration,
            Name = options.Name,
            ShouldHandle = predicateBuilder
        };

        if (options.OnCircuitOpened != null)
        {
            pollyOptions.OnOpened = args => HandleCircuitOpened(options, args.Context, args.BreakDuration, args.Outcome.Exception);
        }
        if (options.OnCircuitClosed != null)
        {
            pollyOptions.OnClosed = args => HandleCircuitClosed(options, args.Context, args.Outcome.Exception);
        }
        if (options.OnCircuitHalfOpened != null)
        {
            pollyOptions.OnHalfOpened = args => HandleCircuitHalfOpened(options, args.Context);
        }

        builder.AddCircuitBreaker(pollyOptions);
    }

    private static async ValueTask HandleCircuitOpened(EcoOptions.CircuitBreakerStrategyOptions options, global::Polly.ResilienceContext context, TimeSpan breakDuration, Exception? exception)
    {
        if (options.OnCircuitOpened != null)
        {
            var ecoCtx = PollyContextAdapter.GetEcosystemContext(context);
            var ctx = new EcoOptions.CircuitBreakerStateContext(
                ecoCtx,
                EcoOptions.CircuitBreakerState.Open,
                breakDuration,
                exception);
            await options.OnCircuitOpened(ctx).ConfigureAwait(false);
        }
    }

    private static async ValueTask HandleCircuitClosed(EcoOptions.CircuitBreakerStrategyOptions options, global::Polly.ResilienceContext context, Exception? exception)
    {
        if (options.OnCircuitClosed != null)
        {
            var ecoCtx = PollyContextAdapter.GetEcosystemContext(context);
            var ctx = new EcoOptions.CircuitBreakerStateContext(
                ecoCtx,
                EcoOptions.CircuitBreakerState.Closed,
                null,
                exception);
            await options.OnCircuitClosed(ctx).ConfigureAwait(false);
        }
    }

    private static async ValueTask HandleCircuitHalfOpened(EcoOptions.CircuitBreakerStrategyOptions options, global::Polly.ResilienceContext context)
    {
        if (options.OnCircuitHalfOpened != null)
        {
            var ecoCtx = PollyContextAdapter.GetEcosystemContext(context);
            var ctx = new EcoOptions.CircuitBreakerStateContext(
                ecoCtx,
                EcoOptions.CircuitBreakerState.HalfOpen);
            await options.OnCircuitHalfOpened(ctx).ConfigureAwait(false);
        }
    }

    private static void AddRateLimiter(global::Polly.ResiliencePipelineBuilder builder, EcoOptions.RateLimiterStrategyOptions options)
    {
        var limiter = ResolveRateLimiter(options);
        builder.AddRateLimiter(limiter);
    }

    private static void AddRateLimiterTyped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(global::Polly.ResiliencePipelineBuilder<TResult> builder, EcoOptions.RateLimiterStrategyOptions options)
    {
        var limiter = ResolveRateLimiter(options);
        builder.AddRateLimiter(limiter);
    }

    private static RateLimiter ResolveRateLimiter(EcoOptions.RateLimiterStrategyOptions options)
    {
        if (options.CustomRateLimiter != null)
        {
            return options.CustomRateLimiter;
        }

        return options.LimiterType switch
        {
            EcoOptions.RateLimiterType.FixedWindow => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                QueueLimit = options.QueueLimit,
                Window = options.Window,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }),
            EcoOptions.RateLimiterType.Concurrency => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                QueueLimit = options.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }),
            EcoOptions.RateLimiterType.TokenBucket => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = options.PermitLimit,
                QueueLimit = options.QueueLimit,
                ReplenishmentPeriod = options.Window,
                TokensPerPeriod = options.PermitLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }),
            _ => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                QueueLimit = options.QueueLimit,
                Window = options.Window,
                SegmentsPerWindow = 4,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            })
        };
    }

    private static void AddFallbackTyped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(global::Polly.ResiliencePipelineBuilder<TResult> builder, EcoOptions.FallbackStrategyOptions<TResult> options)
    {
        var predicateBuilder = new PredicateBuilder<TResult>().Handle<Exception>(ex =>
            options.ShouldHandleException?.Invoke(ex) ?? TransientExceptionClassifier.IsTransient(ex));

        if (options.ShouldHandleResult != null)
        {
            predicateBuilder.HandleResult(res => options.ShouldHandleResult(res));
        }

        var pollyOptions = new global::Polly.Fallback.FallbackStrategyOptions<TResult>
        {
            Name = options.Name,
            ShouldHandle = predicateBuilder,
            FallbackAction = async args =>
            {
                var ecoCtx = PollyContextAdapter.GetEcosystemContext(args.Context)
                    ?? ResilienceContext.Create(options.Name ?? "Fallback", args.Context.CancellationToken);

                var fallbackCtx = new EcoOptions.FallbackContext(ecoCtx, args.Outcome.Exception, args.Outcome.Result);
                if (options.OnFallback != null)
                {
                    await options.OnFallback(fallbackCtx).ConfigureAwait(false);
                }

                if (options.FallbackAction != null)
                {
                    var fallbackResult = await options.FallbackAction(fallbackCtx).ConfigureAwait(false);
                    return Outcome.FromResult(fallbackResult);
                }

                return Outcome.FromResult(default(TResult)!);
            }
        };

        builder.AddFallback(pollyOptions);
    }

    private static void AddHedgingParallel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(global::Polly.ResiliencePipelineBuilder<TResult> builder, EcoOptions.HedgingStrategyOptions<TResult> options)
    {
        var predicateBuilder = new PredicateBuilder<TResult>().Handle<Exception>(ex =>
            options.ShouldHandleException?.Invoke(ex) ?? TransientExceptionClassifier.IsTransient(ex));

        if (options.ShouldHandleResult != null)
        {
            predicateBuilder.HandleResult(res => options.ShouldHandleResult(res));
        }

        var pollyOptions = new global::Polly.Hedging.HedgingStrategyOptions<TResult>
        {
            MaxHedgedAttempts = options.MaxHedgedAttempts,
            Delay = options.Delay,
            Name = options.Name,
            ShouldHandle = predicateBuilder
        };

        if (options.OnHedging != null)
        {
            pollyOptions.OnHedging = async args =>
            {
                var ecoCtx = PollyContextAdapter.GetEcosystemContext(args.ActionContext)
                    ?? ResilienceContext.Create(options.Name ?? "Hedging", args.ActionContext.CancellationToken);
                var hedgeCtx = new EcoOptions.HedgingContext(ecoCtx, args.AttemptNumber);
                await options.OnHedging(hedgeCtx).ConfigureAwait(false);
            };
        }

        builder.AddHedging(pollyOptions);
    }

    private static void AddHedgingFallback(global::Polly.ResiliencePipelineBuilder builder, EcoOptions.HedgingStrategyOptions options)
    {
        var retryOptions = new global::Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = options.MaxHedgedAttempts,
            Delay = options.Delay,
            BackoffType = DelayBackoffType.Constant,
            UseJitter = false,
            Name = options.Name ?? "Hedging",
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                options.ShouldHandleException?.Invoke(ex) ?? TransientExceptionClassifier.IsTransient(ex))
        };

        builder.AddRetry(retryOptions);
    }

    private static void AddHedgingFallbackTyped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(global::Polly.ResiliencePipelineBuilder<TResult> builder, EcoOptions.HedgingStrategyOptions options)
    {
        var retryOptions = new global::Polly.Retry.RetryStrategyOptions<TResult>
        {
            MaxRetryAttempts = options.MaxHedgedAttempts,
            Delay = options.Delay,
            BackoffType = DelayBackoffType.Constant,
            UseJitter = false,
            Name = options.Name ?? "Hedging",
            ShouldHandle = new PredicateBuilder<TResult>().Handle<Exception>(ex =>
                options.ShouldHandleException?.Invoke(ex) ?? TransientExceptionClassifier.IsTransient(ex))
        };

        builder.AddRetry(retryOptions);
    }
}
