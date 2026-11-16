namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.Exceptions;

/// <summary>
/// Thrown when a connection cannot be released back to the database connection pool.
/// </summary>
/// <param name="innerException">The exception raised while releasing the connection.</param>
public class DbConnectionReleaseException(Exception innerException)
    : DbPolicyExecutionException("Error releasing connection", innerException);
