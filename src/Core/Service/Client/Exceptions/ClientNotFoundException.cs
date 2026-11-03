using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Service.Client.Exceptions;

/// <summary>
/// Thrown when no client can be resolved for a given key.
/// </summary>
public sealed class ClientNotFoundException : NotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientNotFoundException"/> class.
    /// </summary>
    /// <param name="key">The key for which no client was found.</param>
    public ClientNotFoundException(string key)
        : base($"No client found for key '{key}'.")
    {
        Key = key;
    }

    /// <summary>
    /// Gets the key for which no client was found.
    /// </summary>
    public string Key { get; }
}
