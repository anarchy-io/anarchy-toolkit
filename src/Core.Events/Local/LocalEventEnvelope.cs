using Anarchy.Toolkit.Core.Helper;

namespace Anarchy.Toolkit.Core.Events.Local;

/// <summary>
/// Convenience base record for in-process events published through
/// <see cref="Local.ILocalEventBus"/>.
/// </summary>
/// <example>
/// <code language="csharp">
/// public sealed record UserRegistered(string UserId) : LocalEventEnvelope;
///
/// await localEventBus.PublishAsync(new UserRegistered("42") { EventSource = "signup" });
/// </code>
/// </example>
public record LocalEventEnvelope : ILocalEvent
{
    /// <summary>
    /// When the event was created (UTC). Defaults to the instantiation time.
    /// </summary>
    public DateTimeOffset EventMoment = DateHelper.Now();

    /// <summary>
    /// Optional free-form description of who or what raised the event.
    /// </summary>
    public string? EventSource = null;
}
