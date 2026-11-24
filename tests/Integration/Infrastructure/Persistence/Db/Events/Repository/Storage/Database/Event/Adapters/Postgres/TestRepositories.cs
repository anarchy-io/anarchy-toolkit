using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Repository.Storage.Database.Postgres;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Storage.Database.Postgres;
using Npgsql;

namespace Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.Adapters.Postgres;

internal sealed class TestEventWriter(IStoragePolicy<NpgsqlConnection> policy, AppTelemetry telemetry)
    : TestDomainEventWriter<NpgsqlConnection, PostgresDbDialect>(policy, telemetry);

internal sealed class TestEventReader(IStoragePolicy<NpgsqlConnection> policy, AppTelemetry telemetry)
    : DbDomainEventReaderRepositoryBase<NpgsqlConnection, PostgresDbDialect>(policy, telemetry);

internal sealed class TestOrderWriter(IStoragePolicy<NpgsqlConnection> policy, AppTelemetry telemetry)
    : PostgresDbBatchWriterRepositoryBase<NewTestOrder>(policy, telemetry)
{
    public override Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null) => throw new NotSupportedException();
    protected override Task<object> ToSaveInput(NewTestOrder entity, CancellationToken? cancellationToken = null) => Task.FromResult<object>(entity);
    public override Task<int> SaveAllAsync(IReadOnlyCollection<NewTestOrder> entities, CancellationToken? cancellationToken = null) =>
        SaveAllAsync("TestOrder.Save.All", entities, cancellationToken);
}

internal sealed class TestOrderReader(IStoragePolicy<NpgsqlConnection> policy, AppTelemetry telemetry)
    : PostgresDbReaderRepositoryBase<TestOrder>(policy, telemetry)
{
    public override Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null) => throw new NotSupportedException();
    public override Task<TestOrder> GetByIdAsync(long id, CancellationToken? cancellationToken = null) => GetByIdAsync("TestOrder.Get.ById", id, cancellationToken);
    public override Task<IReadOnlyCollection<TestOrder>> GetAllAsync(int? limit = null, int? offset = null, CancellationToken? cancellationToken = null) =>
        GetAllAsync("TestOrder.Get.All", limit, offset, cancellationToken);
}
