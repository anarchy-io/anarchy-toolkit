using Anarchy.Toolkit.Core.Health.Entity.Service;

namespace Anarchy.Toolkit.Core.Health.Service.Health;

public interface IServiceHealthReport
{
    public Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null);
}
