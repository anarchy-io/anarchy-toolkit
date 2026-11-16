using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;

/// <summary>
/// Settings of the HTTP health-check endpoint exposed by workers.
/// </summary>
public sealed record WorkerHealthCheckSettings
{
    /// <summary>The TCP port the health-check API listens on. Required.</summary>
    [Required] public int ServicePort { get; init; }
}

/// <summary>
/// Capability interface for application settings that contain a <c>WorkerHealthCheck</c> section.
/// </summary>
public interface IWorkerHealthCheckAppSettings : IAppSettings
{
    /// <summary>The worker health-check settings (configuration section <c>WorkerHealthCheck</c>). Required.</summary>
    [Required] public WorkerHealthCheckSettings WorkerHealthCheck { get; init; }
}
