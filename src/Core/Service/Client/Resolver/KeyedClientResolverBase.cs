using Anarchy.Toolkit.Core.Service.Client.Exceptions;

namespace Anarchy.Toolkit.Core.Service.Client.Resolver;

/// <summary>
/// A generic base resolver that maps a set of clients to string keys and
/// resolves them on demand.
/// </summary>
/// <typeparam name="T">The type of client being resolved.</typeparam>
public abstract class KeyedClientResolverBase<T> : IClientResolver<T>
{
    private readonly Dictionary<string, T> _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedClientResolverBase{T}"/> class.
    /// </summary>
    /// <param name="clients">The clients to index by key.</param>
    /// <param name="keySelector">A function that extracts the key from a client.</param>
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
    /// <exception cref="ClientNotFoundException">Thrown when no client matches the key.</exception>
    public T GetClient(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _map.TryGetValue(key, out var client)
            ? client
            : throw new ClientNotFoundException(key);
    }
}
