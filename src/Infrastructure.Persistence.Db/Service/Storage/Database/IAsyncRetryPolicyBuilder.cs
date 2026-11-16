using Polly;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Builds the Polly <see cref="ResiliencePipeline" /> used by <see cref="ResilienceDbPolicy{TDbConnection}" /> to
///     retry database operations.
/// </summary>
/// <typeparam name="T">The database connection type the pipeline is associated with (used to distinguish registrations).</typeparam>
public interface IAsyncRetryPolicyBuilder<T>
{
    /// <summary>Builds a new resilience pipeline.</summary>
    /// <returns>The resilience pipeline.</returns>
    ResiliencePipeline Build();
}
