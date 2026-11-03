using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.Exceptions;

public class DbPolicyExecutionException : DsPolicyExecutionException
{
    public DbPolicyExecutionException(string failure, Exception? innerException) : base($"DB Policy failure: {failure}",
        innerException)
    { }

    public DbPolicyExecutionException(Exception? innerException) : base(
        "Something went wrong during the Database Policy execution", innerException)
    { }
}
