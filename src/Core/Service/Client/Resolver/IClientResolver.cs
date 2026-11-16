namespace Anarchy.Toolkit.Core.Service.Client.Resolver;

/// <summary>
/// Resolves a client instance by its string key.
/// </summary>
/// <remarks>
/// See <see cref="KeyedClientResolverBase{T}"/> for a ready-made dictionary-based implementation.
/// </remarks>
/// <typeparam name="T">The type of client being resolved.</typeparam>
public interface IClientResolver<out T>
{
    /// <summary>
    /// Gets the client associated with the specified key.
    /// </summary>
    /// <param name="key">The key identifying the client.</param>
    /// <returns>The resolved client.</returns>
    public T GetClient(string key);
}
