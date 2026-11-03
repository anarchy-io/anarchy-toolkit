using Anarchy.Toolkit.Core.Health.Entity.Service;

namespace Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;

public record HealthCheckEntityOutput(
    string Name,
    string Result,
    DateTimeOffset TestedAt,
    TimeSpan Duration,
    ExceptionReportOutput? Problem
)
{
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
