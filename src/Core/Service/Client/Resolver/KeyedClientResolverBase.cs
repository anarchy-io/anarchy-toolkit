using Anarchy.Toolkit.Core.Service.Client.Exceptions;

namespace Anarchy.Toolkit.Core.Service.Client.Resolver;

/// <summary>
/// A generic base resolver that maps a set of clients to string keys and
/// resolves them on demand.
/// </summary>
/// <remarks>
/// The key map is built once in the constructor (keys are compared ordinally and case-sensitively) and is read-only
/// afterwards, so resolution is thread-safe. Typical usage is to inject every registered implementation of a client
/// (e.g. <c>IEnumerable&lt;T&gt;</c> from the DI container) and select one per tenant, region or broker name.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public sealed class StorageClientResolver(IEnumerable&lt;IStorageClient&gt; clients)
///     : KeyedClientResolverBase&lt;IStorageClient&gt;(clients, c =&gt; c.Name);
///
/// var client = resolver.GetClient("primary");
/// </code>
/// </example>
/// <typeparam name="T">The type of client being resolved.</typeparam>
public abstract class KeyedClientResolverBase<T> : IClientResolver<T>
{
    private readonly Dictionary<string, T> _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedClientResolverBase{T}"/> class.
    /// </summary>
    /// <param name="clients">The clients to index by key.</param>
    /// <param name="keySelector">A function that extracts the key from a client.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="clients"/> or <paramref name="keySelector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">Two clients produce the same key.</exception>
    protected KeyedClientResolverBase(IEnumerable<T> clients, Func<T, string> keySelector)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(keySelector);

        _map = clients.ToDictionary(keySelector, c => c);
    }

    /// <summary>
    /// Gets the client associated with the specified key.
    /// </summary>
    /// <param name="key">The key identifying the client.</param>
    /// <returns>The resolved client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ClientNotFoundException">Thrown when no client matches the key.</exception>
    public T GetClient(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _map.TryGetValue(key, out var client)
            ? client
            : throw new ClientNotFoundException(key);
    }
}
