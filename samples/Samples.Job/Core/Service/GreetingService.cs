using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Samples.Job.Infrastructure.Entity.Configuration;

namespace Anarchy.Toolkit.Samples.Job.Core.Service;

public sealed class GreetingService(AppSettings settings, AppTelemetry telemetry)
{
    public Task<string> GreetAsync(string name) =>
        telemetry.MonitoredActionAsync(this.CalledFrom(), () => {
            telemetry.LogInformation(this.CalledFrom(), "Greeting {Name}", name);
            return Task.FromResult($"{settings.Greeting}, {name}!");
        });
}
