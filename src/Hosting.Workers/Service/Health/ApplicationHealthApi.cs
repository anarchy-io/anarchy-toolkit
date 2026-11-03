using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Entity.Api.Output;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Entity.Http;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health
{
    /// <summary>
    /// An API which runs the service health reports and gathers environment information, formatting both as JSON.
    /// The health report has the same shape as the web API's <c>/health/services</c> response.
    /// </summary>
    public class ApplicationHealthApi
    {
        internal IReadOnlyCollection<IServiceHealthReport> Reports { get; }
        internal ApplicationInformation ApplicationInformation { get; }

        /// <summary>
        /// Create a new instance of <see cref="ApplicationHealthApi" /> with required services injected.
        /// </summary>
        public ApplicationHealthApi(
            IReadOnlyCollection<IServiceHealthReport> reports,
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
        public static ApplicationHealthApi WithCustomMetadata(
            IReadOnlyCollection<IServiceHealthReport> reports,
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
        public static ApplicationHealthApi WithMetadataFromAssembly(
            IReadOnlyCollection<IServiceHealthReport> reports,
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
        public Task<HealthCheckReport> GetHealthReportAsync(CancellationToken? cancellationToken = null) =>
            HealthCheckReport.RunAsync(Reports, cancellationToken);

        /// <summary>
        /// Run all service health reports and return the result formatted as JSON, along with an HTTP status code:
        /// OK when every test passed, InternalServerError otherwise (as the web API's health controller does).
        /// </summary>
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
        public string GetApplicationInformationAsJson()
        {
            var dto = ConvertToDto(ApplicationInformation);

            return SerializeDto(dto);
        }

        private ApplicationInformationDto ConvertToDto(ApplicationInformation information) =>
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
}
