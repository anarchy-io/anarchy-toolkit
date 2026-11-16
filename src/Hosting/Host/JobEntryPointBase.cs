using Anarchy.Toolkit.Hosting.Host.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Host;

/// <summary>
/// Base entry point for Job (Lambda) hosts, building the host for the given startup type.
/// Disposing the entry point disposes the host and, with it, every disposable singleton it created.
/// </summary>
/// <remarks>
/// The host is built (but not started) in the constructor via <see cref="AppHostBuilder.ForJob{TJobStartup}"/>.
/// A Lambda function class typically derives from this type, keeps one instance per execution environment and
/// resolves its services from <see cref="App"/> in the handler.
/// </remarks>
/// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
/// <example>
/// <code language="csharp">
/// public sealed class Function : JobEntryPointBase&lt;Startup&gt;
/// {
///     public async Task HandleAsync(SQSEvent sqsEvent, ILambdaContext context)
///     {
///         var handler = App.Services.GetRequiredService&lt;IOrderHandler&gt;();
///         await handler.HandleAsync(sqsEvent);
///     }
/// }
/// </code>
/// </example>
public abstract class JobEntryPointBase<TJobStartup> : IDisposable, IAsyncDisposable
    where TJobStartup : class, IStartup
{
    /// <summary>The built job host.</summary>
    protected readonly IHost App;

    /// <summary>
    /// Builds the job host for <typeparamref name="TJobStartup"/>, calling <see cref="ConfigureAppConfiguration"/>.
    /// </summary>
    protected JobEntryPointBase()
    {
        // ReSharper disable once VirtualMemberCallInConstructor: the hook only adds configuration sources
        App = AppHostBuilder.ForJob<TJobStartup>(ConfigureAppConfiguration);
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
    /// The application name, derived from the last segment of the assembly name.
    /// </summary>
    /// <exception cref="AppNameNotResolvedException">Thrown if the assembly name cannot be resolved.</exception>
    protected virtual string AppName {
        get =>
            GetType().Assembly.GetName().Name?.Split(".").Last() ??
            throw new AppNameNotResolvedException(GetType());
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
