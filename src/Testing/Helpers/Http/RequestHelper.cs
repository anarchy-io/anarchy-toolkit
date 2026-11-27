using System.Text;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.Helpers.Http;

/// <summary>
/// Helpers for building HTTP request content in tests.
/// </summary>
public static class RequestHelper
{
    /// <summary>
    /// Serializes <paramref name="data"/> with <see cref="JsonSerializer"/> (default options) into UTF-8 <c>application/json</c> content.
    /// </summary>
    /// <typeparam name="T">The type of the payload.</typeparam>
    /// <param name="data">The payload to serialize.</param>
    /// <returns>A <see cref="StringContent"/> holding the JSON.</returns>
    public static StringContent CreateJsonContent<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
