using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Builds an exponential back-off retry pipeline that retries the exceptions classified as transient by
///     <typeparamref name="TDialect" />.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection the pipeline is used with.</typeparam>
/// <typeparam name="TDialect">The provider dialect classifying transient exceptions.</typeparam>
/// <param name="options">Resilience options; <see cref="DbResilienceOptions.RetryCount" /> sets the maximum retries.</param>
/// <param name="telemetry">Application telemetry used to log a warning on every retry.</param>
/// <remarks>
///     The delay before retry attempt <c>n</c> (zero-based) is <c>2^(n + 1)</c> seconds, i.e. 2s, 4s, 8s, ...
///     The pipeline is consumed by <see cref="ResilienceDbPolicy{TDbConnection}" />.
/// </remarks>
public class DbAsyncRetryPolicyBuilder<TDbConnection, TDialect>(DbResilienceOptions options, AppTelemetry? telemetry = null)
    : IAsyncRetryPolicyBuilder<TDbConnection>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    /// <inheritdoc />
    public ResiliencePipeline Build()
    {
        var caller = this.CalledFrom();

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions {
                MaxRetryAttempts = options.RetryCount,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(exception => TDialect.IsTransient(exception)),
                DelayGenerator = args =>
                    new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber + 1))),
                OnRetry = args => {
                    var exception = args.Outcome.Exception;
                    if (exception is not null)
                    {
                        telemetry?.Log(
                            LogLevel.Warning,
                            caller,
                            exception,
                            "Retry operation attempt {RetryCount} after {TimeSpan}s because {Exception}",
                            args.AttemptNumber + 1,
                            args.RetryDelay.TotalSeconds,
                            exception.Message
                        );
                    }

                    return default;
                }
            })
            .Build();
    }
}
