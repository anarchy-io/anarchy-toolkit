using Anarchy.Toolkit.Core.Health.Entity.Service;

namespace Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;

/// <summary>API output of a single health check.</summary>
/// <param name="Name">The checked service name.</param>
/// <param name="Result">The check result.</param>
/// <param name="TestedAt">When the check ran.</param>
/// <param name="Duration">How long the check took.</param>
/// <param name="Problem">The failure details, or <see langword="null"/> when the check succeeded.</param>
public record HealthCheckEntityOutput(
    string Name,
    string Result,
    DateTimeOffset TestedAt,
    TimeSpan Duration,
    ExceptionReportOutput? Problem
)
{
    /// <summary>Maps a <see cref="HealthCheckEntity"/> to its API output.</summary>
    /// <param name="dto">The health check result.</param>
    /// <returns>The API output.</returns>
    public static HealthCheckEntityOutput FromDto(HealthCheckEntity dto)
    {
        return new HealthCheckEntityOutput(
            Name: dto.Name,
            Result: dto.Result,
            TestedAt: dto.TestedAt,
            Duration: dto.Duration,
            Problem: dto.Problem is null ? null : ExceptionReportOutput.FromDto(dto.Problem)
        );
    }
}
