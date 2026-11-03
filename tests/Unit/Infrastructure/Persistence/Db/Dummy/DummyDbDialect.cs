using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;

/// <summary>
/// Dialect relying on every <see cref="IDbDialect"/> default, treating <see cref="InvalidOperationException"/>
/// as transient.
/// </summary>
public sealed class DummyDbDialect : IDbDialect
{
    public static string Name => "dummy";

    public static bool IsNoData(Exception exception) => false;

    public static bool IsUniqueViolation(Exception exception) => false;

    public static bool IsTransient(Exception exception) => exception is InvalidOperationException;
}
