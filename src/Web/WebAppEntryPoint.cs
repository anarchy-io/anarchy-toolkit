using Anarchy.Toolkit.Web.Composition;
using Microsoft.AspNetCore.HttpOverrides;

namespace Anarchy.Toolkit.Web;

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

    public static void RunWebHost<TStartup>(
        string[] args,
        int? portInsecure = null,
        int? portSecure = null) where TStartup : class, IWebAppStartup
    {
        _portInsecure = portInsecure ?? _portInsecure;
        _portSecure = portSecure ?? _portSecure;

        Host.CreateDefaultBuilder(args)
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
    /// <see cref="DefaultContainerPort"/>. A lightweight <see cref="ContainerHealthPath"/>
    /// endpoint is registered for the container and load-balancer health checks.
    /// </remarks>
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
