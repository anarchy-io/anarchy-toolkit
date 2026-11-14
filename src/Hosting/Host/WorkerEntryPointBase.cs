using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Host;

/// <summary>
/// Base entry point for Worker (EC2/systemd) hosts, building the host for the given startup type.
/// Disposing the entry point disposes the host and, with it, every disposable singleton it created.
/// </summary>
/// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
public abstract class WorkerEntryPointBase<TJobStartup> : IDisposable, IAsyncDisposable
    where TJobStartup : class, IStartup
{
    /// <summary>The built worker host.</summary>
    public readonly IHost App;

    protected WorkerEntryPointBase()
    {
        // ReSharper disable once VirtualMemberCallInConstructor: the hook only adds configuration sources
        App = AppHostBuilder.ForWorker<TJobStartup>(ConfigureAppConfiguration);
    }

    /// <summary>
    /// Adds the application's own configuration sources (e.g. the AWS SSM Parameter Store) before the environment
    /// variables. Called while the host is built, from the constructor, so it must not rely on derived-class state.
    /// </summary>
    protected virtual void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
    {
    }

    public void Dispose()
    {
        App.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (App is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            App.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
