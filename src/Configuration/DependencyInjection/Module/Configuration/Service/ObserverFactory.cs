using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Configuration.Extension.Observability;
using Anarchy.Toolkit.Configuration.Host;
using Anarchy.Toolkit.Configuration.Service.Observability;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;

/// <summary>
/// What an <see cref="ObserverFactory"/> knows about the application it builds an <see cref="Observer"/> for.
/// </summary>
/// <param name="Services">The application's service provider.</param>
/// <param name="Configuration">The application's configuration.</param>
/// <param name="Environment">The hosting environment.</param>
/// <param name="AppName">The resolved application name (see <see cref="ObservabilityOptions.AppNamePrefix"/>).</param>
/// <param name="Runner">How the application runs, when hosted through <c>Hosting</c>.</param>
public sealed record ObserverContext(
    IServiceProvider Services,
    IConfiguration Configuration,
    IHostEnvironment Environment,
    string AppName,
    AppRunner.Types? Runner);

/// <summary>
/// Builds the application's <see cref="Observer"/>, vendor-neutral by default: readable console logs and
/// <see cref="ConsoleMeasurer"/> locally; JSON console logs and <see cref="LoggerMeasurer"/> once hosted.
/// </summary>
/// <remarks>
/// Every step is virtual, so an organisation derives from this factory and overrides only what differs: its log
/// properties (<see cref="Enrich"/>), sinks (<see cref="WriteTo"/>), metrics backend (<see cref="CreateMeasurer"/>),
/// or the whole hosted/local behaviour.
/// </remarks>
public class ObserverFactory
{
    /// <summary>The log property holding the correlation id.</summary>
    protected virtual string CorrelationIdPropertyName => CorrelationLogPropertyNames.CorrelationId;

    /// <summary>The log property holding the attempt id.</summary>
    protected virtual string AttemptIdPropertyName => CorrelationLogPropertyNames.AttemptId;

    public virtual Observer Create(ObserverContext context)
    {
        return context.Environment.IsLocal() ? CreateLocal(context) : CreateHosted(context);
    }

    /// <summary>Builds the observer used on a developer machine.</summary>
    protected virtual Observer CreateLocal(ObserverContext context)
    {
        return new Observer(
            new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("app_name", context.AppName)
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Literate, restrictedToMinimumLevel: LogEventLevel.Debug)
                .CreateLogger(),
            new ConsoleMeasurer());
    }

    /// <summary>
    /// Builds the observer used once deployed: <see cref="CreateBaseLoggerConfiguration"/>, then
    /// <see cref="Enrich"/>, then <see cref="WriteTo"/>, with metrics from <see cref="CreateMeasurer"/>.
    /// </summary>
    protected virtual Observer CreateHosted(ObserverContext context)
    {
        var logger = WriteTo(Enrich(CreateBaseLoggerConfiguration(context), context), context)
            .CreateLogger();

        return new Observer(logger, CreateMeasurer(context, logger));
    }

    /// <summary>
    /// The baseline every hosted logger starts from: log context, correlation ids, structured exceptions and the
    /// <c>app_name</c> property, minimum level <c>Information</c> with <c>Microsoft</c>/<c>System</c> overrides.
    /// </summary>
    protected virtual LoggerConfiguration CreateBaseLoggerConfiguration(ObserverContext context)
    {
        return new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId(CorrelationIdPropertyName, AttemptIdPropertyName)
            .Enrich.WithStructuredExceptionContext()
            .Enrich.WithProperty("app_name", context.AppName)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Error);
    }

    /// <summary>Adds hosted-only enrichers. By default, the AWS request context on Lambda.</summary>
    protected virtual LoggerConfiguration Enrich(LoggerConfiguration configuration, ObserverContext context)
    {
        return context.Runner is AppRunner.Types.Lambda
            ? configuration.Enrich.WithAmazonRequestContext()
            : configuration;
    }

    /// <summary>Adds the hosted sinks. By default, JSON to the console.</summary>
    protected virtual LoggerConfiguration WriteTo(LoggerConfiguration configuration, ObserverContext context)
    {
        return configuration.WriteTo.Console(new JsonFormatter(renderMessage: true));
    }

    /// <summary>Builds the hosted metrics backend. By default, metrics are written as log events.</summary>
    protected virtual MeasurerBase CreateMeasurer(ObserverContext context, ILogger logger)
    {
        return new LoggerMeasurer(logger);
    }
}
