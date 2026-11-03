using Anarchy.Toolkit.Core.Service.Client.Resolver;

namespace Anarchy.Toolkit.Tests.Unit.Core.Dummy;

public sealed class DummyClientResolver(IEnumerable<IDummyClient> clients)
    : KeyedClientResolverBase<IDummyClient>(clients, client => client.ClientKey);
