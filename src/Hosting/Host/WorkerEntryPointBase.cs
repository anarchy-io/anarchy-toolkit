using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Host;

/// <summary>
/// Base entry point for Worker (EC2/systemd) hosts, building the host for the given startup type.
/// Disposing the entry point disposes the host and, with it, every disposable singleton it created.
/// </summary>
/// <remarks>
/// The host is built (but not started) in the constructor via <see cref="AppHostBuilder.ForWorker{TJobStartup}"/>;
/// run it with <see cref="App"/> (e.g. <c>RunAsync()</c>).
/// </remarks>
/// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
/// <example>
/// <code language="csharp">
/// public sealed class Program : WorkerEntryPointBase&lt;Startup&gt;
/// {
///     public static async Task Main()
///     {
///         await using var program = new Program();
///         await program.App.RunAsync();
///     }
/// }
/// </code>
/// </example>
public abstract class WorkerEntryPointBase<TJobStartup> : IDisposable, IAsyncDisposable
    where TJobStartup : class, IStartup
{
    /// <summary>The built worker host.</summary>
    public readonly IHost App;

    /// <summary>
    /// Builds the worker host for <typeparamref name="TJobStartup"/>, calling <see cref="ConfigureAppConfiguration"/>.
    /// </summary>
    protected WorkerEntryPointBase()
    {
        // ReSharper disable once VirtualMemberCallInConstructor: the hook only adds configuration sources
        App = AppHostBuilder.ForWorker<TJobStartup>(ConfigureAppConfiguration);
    }

    /// <summary>
    /// Adds the application's own configuration sources (e.g. the AWS SSM Parameter Store) before the environment
    /// variables. Called while the host is built, from the constructor, so it must not rely on derived-class state.
    /// </summary>
    /// <param name="context">The host builder context (environment and configuration built so far).</param>
    /// <param name="builder">The configuration builder to add sources to.</param>
    protected virtual void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
    {
    }

    /// <summary>
    /// Disposes the host and every disposable singleton it created.
    /// </summary>
    public void Dispose()
    {
        App.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the host (synchronously when it is not <see cref="IAsyncDisposable"/>) and every
    /// disposable singleton it created.
    /// </summary>
    /// <returns>A task that completes when the host has been disposed.</returns>
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
