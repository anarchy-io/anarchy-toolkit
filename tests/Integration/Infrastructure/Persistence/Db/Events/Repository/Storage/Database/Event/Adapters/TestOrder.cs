using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.Adapters;

internal sealed record NewTestOrder(long Id, string Sku) : IEntity;
internal sealed record TestOrder(long Id, string Sku) : IPersistedEntity;
