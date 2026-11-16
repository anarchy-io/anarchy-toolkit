using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;

/// <summary>
/// Retry, circuit-breaker and timeout settings, bound from the <c>Resilience</c> configuration section (see
/// <see cref="IResilienceAppSettings"/>).
/// </summary>
public class ResilienceSettings
{
    /// <summary>The number of retries after a failed attempt.</summary>
    public int RetryCount { get; set; }

    /// <summary>The delay between retries, in milliseconds.</summary>
    public int RetryDelayMilliseconds { get; set; }

    /// <summary>The seed (base) delay used to compute retry back-off, in milliseconds.</summary>
    public int RetrySeedDelayMilliseconds { get; set; }

    /// <summary>The number of failures allowed before the circuit breaker opens.</summary>
    public int CircuitBreakerAllowedAttempts { get; set; }

    /// <summary>How long the circuit stays open, in milliseconds.</summary>
    public int CircuitBreakerDurationMilliseconds { get; set; }

    /// <summary>The timeout applied to a single operation, in milliseconds.</summary>
    public int OperationTimeoutDurationMilliseconds { get; set; }
}

/// <summary>
/// Capability interface for application settings that contain a <c>Resilience</c> section.
/// </summary>
public interface IResilienceAppSettings : IAppSettings
{
    /// <summary>The resilience settings (configuration section <c>Resilience</c>). Required.</summary>
    [Required] public ResilienceSettings Resilience { get; init; }
}
