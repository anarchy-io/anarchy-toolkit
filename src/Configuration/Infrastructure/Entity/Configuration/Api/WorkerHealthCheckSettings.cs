using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;

public sealed record WorkerHealthCheckSettings
{
    [Required] public int ServicePort { get; init; }
}

public interface IWorkerHealthCheckAppSettings : IAppSettings
{
    [Required] public WorkerHealthCheckSettings WorkerHealthCheck { get; init; }
}
