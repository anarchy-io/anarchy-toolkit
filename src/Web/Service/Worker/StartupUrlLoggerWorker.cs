using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Web.Service.Worker;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Hosted service that, in the <c>Development</c> environment, logs the addresses the server listens on once the
/// application has started.
/// </summary>
/// <remarks>Registered by <c>WebAppStartupBase.ConfigureWebCustomServices</c> in development.</remarks>
/// <param name="server">The server whose addresses are logged.</param>
/// <param name="logger">The observer used for logging.</param>
/// <param name="env">The host environment.</param>
/// <param name="lifetime">The application lifetime, used to wait for <c>ApplicationStarted</c>.</param>
public sealed class StartupUrlLoggerWorker(
    IServer server,
    Observer logger,
    IHostEnvironment env,
    IHostApplicationLifetime lifetime) : IHostedService
{
    /// <summary>Registers the address logging callback on application start (development only).</summary>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>A completed task.</returns>
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

    /// <summary>Does nothing.</summary>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
