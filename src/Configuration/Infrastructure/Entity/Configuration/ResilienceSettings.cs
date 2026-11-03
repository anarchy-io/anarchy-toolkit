using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;

public class ResilienceSettings
{
    public int RetryCount { get; set; }
    public int RetryDelayMilliseconds { get; set; }
    public int RetrySeedDelayMilliseconds { get; set; }
    public int CircuitBreakerAllowedAttempts { get; set; }
    public int CircuitBreakerDurationMilliseconds { get; set; }
    public int OperationTimeoutDurationMilliseconds { get; set; }
}

public interface IResilienceAppSettings : IAppSettings
{
    [Required] public ResilienceSettings Resilience { get; init; }
}
