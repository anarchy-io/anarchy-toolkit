using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;
using System.Data.Common;

namespace Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.Adapters;

// The toolkit intentionally leaves the concrete event-store schema and writer to the application.
internal abstract class TestDomainEventWriter<TConnection, TDialect>(IStoragePolicy<TConnection> policy, AppTelemetry telemetry)
    : DbEventWriterRepositoryBase<TConnection, TDialect, DomainEventEnvelope>(policy, telemetry)
    where TConnection : DbConnection where TDialect : IDbDialect
{
    public override Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null) =>
        throw new NotSupportedException("This test schema has no event sequence.");

    public override Task<int> SaveAllAsync(IReadOnlyCollection<DomainEventEnvelope> entities,
                                           CancellationToken? cancellationToken = null) =>
        SaveAllAsync("DomainEvent.Save.All", entities, cancellationToken);

    protected override async Task<object> ToSaveInput(DomainEventEnvelope entity, CancellationToken? cancellationToken = null) =>
        new {
            TypeAggregateId = await GetIdForTypeAsync(EventTypeTable.TypeAggregate, entity.AggregateType, cancellationToken),
            AggregateId = entity.Subject,
            TypeEventId = await GetIdForTypeAsync(EventTypeTable.TypeEvent, entity.Type, cancellationToken),
            Payload = entity.Data?.ToJson(),
            CustomAttributes = entity.CustomAttributes.ToJson(),
            Moment = entity.Time
        };
}
