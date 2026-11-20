using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.Exceptions;

/// <summary>
/// Base exception for failures of a database policy (see <see cref="DbPolicyBase{TDbConnection}"/>) itself, such as
/// acquiring or releasing connections.
/// </summary>
public class DbPolicyExecutionException : StoragePolicyExecutionException
{
    /// <summary>Creates the exception with a message prefixed by <c>DB Policy failure:</c>.</summary>
    /// <param name="failure">Description of the failure.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public DbPolicyExecutionException(string failure, Exception? innerException) : base($"DB Policy failure: {failure}",
        innerException)
    { }

    /// <summary>Creates the exception with a generic message.</summary>
    /// <param name="innerException">The underlying exception, if any.</param>
    public DbPolicyExecutionException(Exception? innerException) : base(
        "Something went wrong during the Database Policy execution", innerException)
    { }
}
