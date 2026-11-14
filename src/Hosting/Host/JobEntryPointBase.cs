using Anarchy.Toolkit.Hosting.Host.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Host;

/// <summary>
/// Base entry point for Job (Lambda) hosts, building the host for the given startup type.
/// Disposing the entry point disposes the host and, with it, every disposable singleton it created.
/// </summary>
/// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
public abstract class JobEntryPointBase<TJobStartup> : IDisposable, IAsyncDisposable
    where TJobStartup : class, IStartup
{
    /// <summary>The built job host.</summary>
    protected readonly IHost App;

    protected JobEntryPointBase()
    {
        // ReSharper disable once VirtualMemberCallInConstructor: the hook only adds configuration sources
        App = AppHostBuilder.ForJob<TJobStartup>(ConfigureAppConfiguration);
    }

    /// <summary>
    /// Adds the application's own configuration sources (e.g. the AWS SSM Parameter Store) before the environment
    /// variables. Called while the host is built, from the constructor, so it must not rely on derived-class state.
    /// </summary>
    protected virtual void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
    {
    }

    /// <summary>
    /// The application name, derived from the last segment of the assembly name.
    /// </summary>
    protected virtual string AppName {
        get =>
            GetType().Assembly.GetName().Name?.Split(".").Last() ??
            throw new AppNameNotResolvedException(GetType());
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
