using System.Data;
using System.Data.Common;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;

/// <summary>
/// In-memory <see cref="DbConnection"/> that only tracks its open/disposed state and the transactions it began.
/// </summary>
public sealed class DummyDbConnection(string connectionString) : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    public bool IsDisposed { get; private set; }

    public int OpenCount { get; private set; }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString { get; set; } = connectionString;
    public override string Database => "dummy";
    public override string DataSource => "dummy";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public override void Open()
    {
        _state = ConnectionState.Open;
        OpenCount++;
    }

    public override void Close() => _state = ConnectionState.Closed;

    public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

    public List<DummyDbTransaction> Transactions { get; } = [];

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        var transaction = new DummyDbTransaction(this);
        Transactions.Add(transaction);
        return transaction;
    }

    protected override DbCommand CreateDbCommand() => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        _state = ConnectionState.Closed;
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
