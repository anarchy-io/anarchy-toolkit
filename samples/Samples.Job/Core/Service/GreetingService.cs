using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Samples.Job.Infrastructure.Entity.Configuration;

namespace Anarchy.Toolkit.Samples.Job.Core.Service;

public sealed class GreetingService(AppSettings settings, Observer observer)
{
    public Task<string> GreetAsync(string name) =>
        observer.MonitoredActionAsync(this.CalledFrom(), () => {
            observer.LogInformation(this.CalledFrom(), "Greeting {Name}", name);
            return Task.FromResult($"{settings.Greeting}, {name}!");
        });
}
