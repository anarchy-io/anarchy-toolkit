namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.Exceptions;

/// <summary>
/// Thrown when a connection cannot be retrieved from the database connection pool.
/// </summary>
public class DbConnectionAcquisitionException(Exception innerException)
    : DbPolicyExecutionException("Failed to establish connection to the database", innerException);
