using System.Data;
using System.Data.Common;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;

/// <summary>
/// In-memory <see cref="DbTransaction"/> that only tracks its committed/rolled back/disposed state.
/// </summary>
public sealed class DummyDbTransaction(DummyDbConnection connection) : DbTransaction
{
    public bool IsCommitted { get; private set; }

    public bool IsRolledBack { get; private set; }

    public bool IsDisposed { get; private set; }

    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    protected override DbConnection DbConnection => connection;

    public override void Commit() => IsCommitted = true;

    public override void Rollback() => IsRolledBack = true;

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
