using Anarchy.Toolkit.Core.Health.Entity.Api.Output;

namespace Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;

/// <summary>API output of a health check run, returned by <c>GET /health/services</c>.</summary>
/// <param name="Passed">Whether all checks passed.</param>
/// <param name="Duration">Total duration of the run.</param>
/// <param name="Tests">The individual check results.</param>
public record HealthCheckReportOutput(
    bool Passed,
    TimeSpan Duration,
    IReadOnlyCollection<HealthCheckEntityOutput> Tests
)
{
    /// <summary>Maps a <see cref="HealthCheckReport"/> to its API output.</summary>
    /// <param name="dto">The health check report.</param>
    /// <returns>The API output.</returns>
    public static HealthCheckReportOutput FromDto(HealthCheckReport dto)
    {
        return new HealthCheckReportOutput(
            Passed: dto.Passed,
            Duration: dto.Duration,
            Tests: dto.Tests.Select(HealthCheckEntityOutput.FromDto).ToArray()
        );
    }
}
