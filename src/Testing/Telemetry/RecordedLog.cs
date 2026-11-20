using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Testing.Telemetry;

/// <summary>A log event captured by <see cref="InMemoryLoggerProvider"/>.</summary>
/// <param name="Category">The logger category.</param>
/// <param name="Level">The level.</param>
/// <param name="EventId">The event id.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="MessageTemplate">The message template (<c>{OriginalFormat}</c>), when the event has one.</param>
/// <param name="Properties">The event's structured properties.</param>
/// <param name="Scopes">The properties of the scopes open when the event was written, innermost last.</param>
/// <param name="Exception">The exception, if any.</param>
public sealed record RecordedLog(
    string Category,
    LogLevel Level,
    EventId EventId,
    string Message,
    string? MessageTemplate,
    IReadOnlyDictionary<string, object?> Properties,
    IReadOnlyDictionary<string, object?> Scopes,
    Exception? Exception)
{
    /// <summary>Gets a property of the event, falling back to the open scopes.</summary>
    /// <param name="name">The property name.</param>
    /// <returns>The value, or <see langword="null"/> when absent.</returns>
    public object? Property(string name) =>
        Properties.TryGetValue(name, out var value) ? value : Scopes.GetValueOrDefault(name);
}
