using Anarchy.Toolkit.Core.Health.Entity.Api.Output;

namespace Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;

public record HealthCheckReportOutput(
    bool Passed,
    TimeSpan Duration,
    IReadOnlyCollection<HealthCheckEntityOutput> Tests
)
{
    public static HealthCheckReportOutput FromDto(HealthCheckReport dto)
    {
        return new HealthCheckReportOutput(
            Passed: dto.Passed,
            Duration: dto.Duration,
            Tests: dto.Tests.Select(HealthCheckEntityOutput.FromDto).ToArray()
        );
    }
}
