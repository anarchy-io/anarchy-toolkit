using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Web.Service.Worker;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;

public sealed class StartupUrlLoggerWorker(
    IServer server,
    Observer logger,
    IHostEnvironment env,
    IHostApplicationLifetime lifetime) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return Task.CompletedTask;

        lifetime.ApplicationStarted.Register(LogAddresses);

        return Task.CompletedTask;
    }

    private void LogAddresses()
    {
        var caller = this.CalledFrom();

        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null || addresses.Count == 0)
        {
            logger.LogDebug(caller, "Webapp started, but no server addresses were discovered.");
            return;
        }

        logger.LogDebug(caller, "Webapp is listening on:");
        foreach (var address in addresses.OrderBy(x => x))
        {
            logger.LogDebug(caller, " ↳ {Address}", address);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
