using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;
using Polly;
using Polly.Retry;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Builds an exponential back-off retry pipeline that retries the exceptions classified as transient by
///     <typeparamref name="TDialect" />.
/// </summary>
public class DbAsyncRetryPolicyBuilder<TDbConnection, TDialect>(DbResilienceOptions options, Observer observer)
    : IAsyncRetryPolicyBuilder<TDbConnection>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
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
                        observer.LogWarning(
                            (CallSiteEntity)caller,
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
