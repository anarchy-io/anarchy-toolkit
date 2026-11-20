using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Opens provider-neutral structured log scopes.</summary>
/// <remarks>
/// Scopes go through <see cref="ILogger.BeginScope{TState}"/>, so every logging provider that supports scopes (the
/// Microsoft console with <c>IncludeScopes</c>, Serilog, OpenTelemetry) attaches the properties to the events written
/// while the scope is open. Scopes flow across <see langword="await"/>.
/// </remarks>
/// <param name="logger">The logger the scopes are opened on.</param>
/// <example>
/// <code language="csharp">
/// using (telemetry.LogContext.PushProperty("OrderId", order.Id))
/// {
///     telemetry.LogInformation(this.CalledFrom(), "Order received"); // carries OrderId
/// }
/// </code>
/// </example>
public sealed class LogContext(ILogger logger)
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Pushes properties until the returned scope is disposed.</summary>
    /// <param name="properties">The properties to attach.</param>
    /// <returns>A handle that closes the scope.</returns>
    public IDisposable PushProperties(params KeyValuePair<string, object?>[] properties) =>
        _logger.BeginScope(properties) ?? EmptyScope.Instance;

    /// <summary>Pushes a single property until the returned scope is disposed.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A handle that closes the scope.</returns>
    public IDisposable PushProperty(string name, object? value) => PushProperties(new KeyValuePair<string, object?>(name, value));

    private sealed class EmptyScope : IDisposable
    {
        public static EmptyScope Instance { get; } = new();
        public void Dispose() { }
    }
}
