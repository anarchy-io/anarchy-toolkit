using Anarchy.Toolkit.Runtime.Web.Composition;
using Microsoft.AspNetCore.HttpOverrides;

namespace Anarchy.Toolkit.Runtime.Web;

/// <summary>
/// Entry points that run a toolkit web application as a self-hosted process (local Kestrel or container).
/// </summary>
/// <remarks>
/// For AWS Lambda hosting use <c>AwsWebAppEntryPointBase</c> from <c>Anarchy.Toolkit.Runtime.Web.Aws</c> instead.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public static class Program
/// {
///     public static void Main(string[] args) =&gt; WebAppEntryPoint.RunWebHost&lt;Startup&gt;(args);
/// }
/// </code>
/// </example>
public static class WebAppEntryPoint
{
    /// <summary>
    /// Default HTTP port the container host listens on. Matches the port expected by the ECS
    /// <c>PrivateServiceSolution</c> container/target-group health checks (see Anarchy.Cdk.Ecs).
    /// </summary>
    private const int DefaultContainerPort = 3000;

    /// <summary>
    /// Path served by the container-level health endpoint. Must match the health check path
    /// configured by the ECS service construct.
    /// </summary>
    private const string ContainerHealthPath = "/health/instance";

    private static int _portInsecure = 5000;
    private static int _portSecure = 5001;

    private static void ConfigureWebHost<TStartup>(IWebHostBuilder builderWebHost) where TStartup : class, IWebAppStartup
    {
        builderWebHost.UseStartup<TStartup>();
        builderWebHost.ConfigureKestrel(options => {
            options.ListenLocalhost(_portInsecure);
            options.ListenLocalhost(_portSecure, x => x.UseHttps());
        });
    }

    /// <summary>
    /// Builds and runs a generic host with Kestrel listening on <c>localhost</c> over HTTP and HTTPS, using
    /// <typeparamref name="TStartup"/> as the startup class. Blocks until the host shuts down.
    /// </summary>
    /// <remarks>
    /// Intended for local development. Uses <see cref="Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(string[])"/> with the content root
    /// set to <see cref="AppContext.BaseDirectory"/>. Ports default to <c>5000</c> (HTTP) and <c>5001</c> (HTTPS).
    /// </remarks>
    /// <typeparam name="TStartup">The startup class.</typeparam>
    /// <param name="args">The process arguments.</param>
    /// <param name="portInsecure">The HTTP port; defaults to <c>5000</c>.</param>
    /// <param name="portSecure">The HTTPS port; defaults to <c>5001</c>.</param>
    public static void RunWebHost<TStartup>(
        string[] args,
        int? portInsecure = null,
        int? portSecure = null) where TStartup : class, IWebAppStartup
    {
        _portInsecure = portInsecure ?? _portInsecure;
        _portSecure = portSecure ?? _portSecure;

        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureWebHostDefaults(ConfigureWebHost<TStartup>)
            .Build()
            .Run()
            ;
    }

    /// <summary>
    /// Runs the web application as a long-lived container process listening on all interfaces
    /// (<c>0.0.0.0</c>) over plain HTTP. This is the entry point used when the application is
    /// published as an ECS Fargate service behind an internal load balancer.
    /// </summary>
    /// <remarks>
    /// Configuration mirrors the AWS Lambda entry point: the application's own sources (e.g. the AWS
    /// Systems Manager Parameter Store) added by <paramref name="configureAppConfiguration"/>, followed by
    /// environment variables. The listening port can be
    /// overridden with the <c>PORT</c> environment variable; it defaults to
    /// <c>3000</c>. A lightweight <c>/health/instance</c>
    /// endpoint is registered for the container and load-balancer health checks. Forwarded headers
    /// (<c>X-Forwarded-For</c>, <c>X-Forwarded-Proto</c>) are honoured from any proxy, since the container is
    /// expected to run behind a TLS-terminating load balancer in a trusted network. The startup is instantiated
    /// via its (<see cref="IWebHostEnvironment"/>, <see cref="IConfiguration"/>) constructor. Blocks until shutdown.
    /// </remarks>
    /// <typeparam name="TStartup">The startup class.</typeparam>
    /// <param name="args">The process arguments.</param>
    /// <param name="configureAppConfiguration">Adds the application's own configuration sources.</param>
    public static void RunContainerWebHost<TStartup>(
        string[] args,
        Action<IConfigurationBuilder, IHostEnvironment>? configureAppConfiguration = null)
        where TStartup : class, IWebAppStartup
    {
        var port = ResolveContainerPort();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        configureAppConfiguration?.Invoke(builder.Configuration, builder.Environment);
        builder.Configuration.AddEnvironmentVariables();

        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        // The container runs plain HTTP behind a TLS-terminating internal ALB. Honour the
        // X-Forwarded-Proto / X-Forwarded-For headers it sets so ASP.NET Core reconstructs the
        // original HTTPS scheme. Without this, scheme-dependent behaviour (HTTPS redirects,
        // generated URLs, and the SameAsRequest cookie SecurePolicy dropping the Secure flag)
        // would be wrong. The ALB and tasks live in trusted private subnets, so we clear the
        // known-network/proxy allow-lists rather than enumerating ephemeral task IPs.
        builder.Services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var startup = (TStartup)Activator.CreateInstance(typeof(TStartup), builder.Environment, builder.Configuration)!;
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();

        // Must run before the startup pipeline (which invokes UseHttpsRedirection and the auth
        // middleware) so downstream middleware observes the forwarded scheme.
        app.UseForwardedHeaders();

        app.MapGet(ContainerHealthPath, () => Results.Ok(new { status = "healthy" }));

        startup.Configure(app);

        app.Run();
    }

    private static int ResolveContainerPort()
    {
        var rawPort = Environment.GetEnvironmentVariable("PORT");

        return int.TryParse(rawPort, out var parsedPort) && parsedPort > 0
            ? parsedPort
            : DefaultContainerPort;
    }
}
