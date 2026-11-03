using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Model;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event;

/// <summary>
/// Static, in-memory cache mapping outbox type-table names to their integer identifiers,
/// partitioned by <see cref="EventTypeTable"/>.
/// </summary>
/// <remarks>
/// The underlying container is shared by every <c>AttributeIdCacheBase&lt;EventTypeTable, int&gt;</c> subclass, so all
/// providers within one process share these type ids.
/// </remarks>
public class EventWriterMultiAttributeIdCache : AttributeIdCacheBase<EventTypeTable, int>;
