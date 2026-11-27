using Anarchy.Toolkit.Bootstrap.Core.Service;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;

namespace Anarchy.Toolkit.Bootstrap.Telemetry;

/// <summary>
/// Default <see cref="IApplicationContext"/> reading from
/// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service.CorrelationContext"/>.
/// </summary>
/// <remarks>
/// Stateless: values are read from async-local storage on each access, so a singleton instance is safe.
/// </remarks>
public class ApplicationContext : IApplicationContext
{
    /// <summary>
    /// Correlation id used when no correlation id is set for the current flow. Generated once per process.
    /// </summary>
    public static readonly string FallbackCorrelationId = Guid.NewGuid()
        .ToString();

    /// <summary>
    /// Attempt id used when no attempt id is set for the current flow. Generated once per process.
    /// </summary>
    public static readonly string FallbackAttemptId = Guid.NewGuid()
        .ToString();

    /// <inheritdoc />
    public string RequestCorrelationId {
        get => CorrelationContext.CorrelationId ?? FallbackCorrelationId;
    }

    /// <inheritdoc />
    public string RequestAttemptId {
        get => CorrelationContext.AttemptId ?? FallbackAttemptId;
    }
}
