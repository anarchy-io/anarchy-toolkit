using Anarchy.Toolkit.Core.Health.Checks;

namespace Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;

/// <summary>API output of a single health check.</summary>
/// <param name="Name">The checked service name.</param>
/// <param name="Result">The check result.</param>
/// <param name="TestedAt">When the check ran.</param>
/// <param name="Duration">How long the check took.</param>
/// <param name="Problem">The failure details, or <see langword="null"/> when the check succeeded.</param>
public record HealthCheckOutcomeOutput(
    string Name,
    string Result,
    DateTimeOffset TestedAt,
    TimeSpan Duration,
    ExceptionReportOutput? Problem
)
{
    /// <summary>Maps a <see cref="HealthCheckOutcome"/> to its API output.</summary>
    /// <param name="dto">The health check result.</param>
    /// <returns>The API output.</returns>
    public static HealthCheckOutcomeOutput FromDto(HealthCheckOutcome dto)
    {
        return new HealthCheckOutcomeOutput(
            Name: dto.Name,
            Result: dto.Result,
            TestedAt: dto.TestedAt,
            Duration: dto.Duration,
            Problem: dto.Problem is null ? null : ExceptionReportOutput.FromDto(dto.Problem)
        );
    }
}
