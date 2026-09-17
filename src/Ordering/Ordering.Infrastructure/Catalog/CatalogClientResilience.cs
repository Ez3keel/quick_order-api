using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Ordering.Infrastructure.Catalog;

/// <summary>
/// The resilience pipeline for calls to Catalog, applied to the HttpClient itself
/// (Microsoft.Extensions.Http.Resilience wraps every request transparently — nothing
/// in HttpCatalogClient's own code decides to retry). Three strategies, each solving
/// a different failure shape:
///   - per-attempt timeout: a single slow request shouldn't hang the order forever;
///   - retry with exponential backoff + jitter: absorbs a blip (one dropped packet,
///     one slow GC pause on Catalog) without the caller ever seeing it;
///   - circuit breaker: absorbs a sustained outage — once Catalog is genuinely down,
///     retrying every single order placement just adds load to a service that's
///     already struggling and makes every customer wait through 3 retries before
///     failing anyway. The breaker fails fast instead, and gives Catalog room to
///     recover by not hammering it while it's down.
/// </summary>
public static class CatalogClientResilience
{
    public static IHttpResiliencePipelineBuilder AddCatalogResilience(this IHttpClientBuilder builder) =>
        builder.AddResilienceHandler("catalog-client", (pipeline, context) =>
        {
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Ordering.Infrastructure.Catalog.Resilience");

            pipeline.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(3),
                OnTimeout = args =>
                {
                    logger.LogWarning("Catalog call timed out after {Timeout}", args.Timeout);
                    return default;
                },
            });

            pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => (int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Retrying Catalog call (attempt {AttemptNumber}) after {Delay} due to {Reason}",
                        args.AttemptNumber + 1, args.RetryDelay, args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                    return default;
                },
            });

            pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => (int)response.StatusCode >= 500),
                OnOpened = args =>
                {
                    logger.LogError("Circuit breaker OPENED for Catalog calls — failing fast for {BreakDuration}", args.BreakDuration);
                    return default;
                },
                OnClosed = _ =>
                {
                    logger.LogInformation("Circuit breaker CLOSED for Catalog calls — requests flowing normally again");
                    return default;
                },
                OnHalfOpened = _ =>
                {
                    logger.LogInformation("Circuit breaker HALF-OPEN for Catalog calls — testing with one request");
                    return default;
                },
            });
        });
}
