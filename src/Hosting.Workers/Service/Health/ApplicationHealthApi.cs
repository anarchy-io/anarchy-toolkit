using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Health.Reports;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Entity.Http;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health;

/// <summary>
/// An API which runs the service health reports and gathers environment information, formatting both as JSON.
/// The health report has the same shape as the web API's <c>/health/services</c> response.
/// </summary>
/// <remarks>
/// Backs the health middlewares (see
/// <see cref="Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware.ApplicationHealthMiddlewareBase"/>). Registered
/// as transient by <c>AddApplicationHealthEndpoints</c>, or created with <see cref="WithCustomMetadata"/> /
/// <see cref="WithMetadataFromAssembly"/>.
/// </remarks>
public class ApplicationHealthApi
{
    internal IReadOnlyCollection<IHealthCheck> Reports { get; }
    internal ApplicationInformation ApplicationInformation { get; }

    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthApi" /> with required services injected.
    /// </summary>
    /// <param name="reports">The service health reports to run.</param>
    /// <param name="applicationInformation">The application and build information.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="reports"/> or <paramref name="applicationInformation"/> is <see langword="null"/>.
    /// </exception>
    public ApplicationHealthApi(
        IReadOnlyCollection<IHealthCheck> reports,
        ApplicationInformation applicationInformation
    )
    {
        Reports = reports ?? throw new ArgumentNullException(nameof(reports));
        ApplicationInformation = applicationInformation
                                 ?? throw new ArgumentNullException(nameof(applicationInformation));
    }

    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthApi" /> with a custom build metadata provider.
    /// </summary>
    /// <param name="reports">The service health reports to run.</param>
    /// <param name="buildMetadata">The build metadata provider.</param>
    /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
    /// <returns>The new health API.</returns>
    public static ApplicationHealthApi WithCustomMetadata(
        IReadOnlyCollection<IHealthCheck> reports,
        IBuildMetadata buildMetadata,
        DateTimeOffset applicationStartTime
    )
    {
        var applicationInformation = ApplicationInformation.WithCustomMetadata(buildMetadata, applicationStartTime);

        return new ApplicationHealthApi(reports, applicationInformation);
    }

    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthApi" /> with build metadata from the provided assembly.
    /// </summary>
    /// <param name="reports">The service health reports to run.</param>
    /// <param name="assembly">The assembly containing the embedded build metadata.</param>
    /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
    /// <returns>The new health API.</returns>
    public static ApplicationHealthApi WithMetadataFromAssembly(
        IReadOnlyCollection<IHealthCheck> reports,
        Assembly assembly,
        DateTimeOffset applicationStartTime
    )
    {
        var applicationInformation = ApplicationInformation.WithMetadataFromAssembly(assembly, applicationStartTime);

        return new ApplicationHealthApi(reports, applicationInformation);
    }

    /// <summary>
    /// Run all service health reports and aggregate their results.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The aggregated health report.</returns>
    public Task<HealthCheckReport> GetHealthReportAsync(CancellationToken? cancellationToken = null) =>
        HealthCheckReport.RunAsync(Reports, cancellationToken);

    /// <summary>
    /// Run all service health reports and return the result formatted as JSON, along with an HTTP status code:
    /// OK when every test passed, InternalServerError otherwise (as the web API's health controller does).
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The JSON report and status code.</returns>
    public async Task<ApplicationHealthHttpResponse> GetHealthHttpResponse(
        CancellationToken? cancellationToken = null)
    {
        var report = await GetHealthReportAsync(cancellationToken);

        return new ApplicationHealthHttpResponse(
            content: report.ToJson(),
            statusCode: report.Passed ? HttpStatusCode.OK : HttpStatusCode.InternalServerError
        );
    }

    private static string SerializeDto(object dto) => System.Text.Json.JsonSerializer.Serialize(dto);

    /// <summary>
    /// Construct an overview of an application's environment and build process and return it formatted as JSON.
    /// </summary>
    /// <remarks>
    /// Uses snake_case property names such as <c>artifact_id</c>, <c>version</c>, <c>git_sha1</c>, <c>up_since</c>,
    /// <c>up_duration</c> (seconds) and <c>machine_name</c>.
    /// </remarks>
    /// <returns>The application information as JSON.</returns>
    public string GetApplicationInformationAsJson()
    {
        var dto = ConvertToDto(ApplicationInformation);

        return SerializeDto(dto);
    }

    private static ApplicationInformationDto ConvertToDto(ApplicationInformation information) =>
        new() {
            ApplicationName = information.ApplicationName,
            ApplicationStartTime = information.ApplicationStartTime,
            ApplicationVersion = information.ApplicationVersion,
            RuntimeVersion = information.RuntimeVersion,
            TargetFramework = information.TargetFramework,
            BuildCommitHash = information.BuildCommitHash,
            BuildMachineName = information.BuildMachineName,
            BuildTimestamp = information.BuildTimestamp,
            BuildUsername = information.BuildUsername,
            BuildNumber = information.BuildNumber,
            MachineName = information.MachineName,
            RunbookUri = information.RunbookUri,
            SystemTime = information.SystemTime,
            UptimeInSeconds = (int)information.Uptime.TotalSeconds,
            OperatingSystem = information.OperatingSystem,
        };

    private class ApplicationInformationDto
    {
        [JsonPropertyName("artifact_id")]
        public required string ApplicationName { get; set; }

        [JsonPropertyName("build_number")]
        public string? BuildNumber { get; set; }

        [JsonPropertyName("build_machine")]
        public string? BuildMachineName { get; set; }

        [JsonPropertyName("built_by")]
        public string? BuildUsername { get; set; }

        [JsonPropertyName("built_when")]
        public DateTimeOffset? BuildTimestamp { get; set; }

        [JsonPropertyName("current_time")]
        public DateTimeOffset SystemTime { get; set; }

        [JsonPropertyName("git_sha1")]
        public string? BuildCommitHash { get; set; }

        [JsonPropertyName("machine_name")]
        public required string MachineName { get; set; }

        [JsonPropertyName("runbook_uri")]
        public string? RunbookUri { get; set; }

        [JsonPropertyName("up_duration")]
        public int UptimeInSeconds { get; set; }

        [JsonPropertyName("up_since")]
        public DateTimeOffset ApplicationStartTime { get; set; }

        [JsonPropertyName("version")]
        public required string ApplicationVersion { get; set; }

        [JsonPropertyName("operating_system")]
        public required string OperatingSystem { get; set; }

        [JsonPropertyName("runtime_version")]
        public required string RuntimeVersion { get; set; }

        [JsonPropertyName("target_framework")]
        public string? TargetFramework { get; set; }
    }
}
