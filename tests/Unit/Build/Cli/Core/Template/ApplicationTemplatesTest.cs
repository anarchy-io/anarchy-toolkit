using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Template;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Core.Template;

public class ApplicationTemplatesTest
{
    [Fact]
    public void RenderApi_UsesApplicationTelemetryInThePingController()
    {
        var context = new ApplicationContext(new ApplicationSpec(ApplicationKind.Api, "Orders"), "Orders.Api", "Orders.Api",
            null, (5000, 5001), null);

        var files = ApplicationTemplates.Render(context);

        files["Presentation/Controller/PingController.cs"]
            .Should().Contain("IAppTelemetry").And.Contain("telemetry.Log(LogLevel.Information")
            .And.NotContain("Observer");
    }

    [Fact]
    public void RenderJob_UsesApplicationTelemetryForMonitoredWork()
    {
        var context = new ApplicationContext(new ApplicationSpec(ApplicationKind.Job, "Processor"), "Processor.Job", "Processor.Job",
            null, null, null);

        var files = ApplicationTemplates.Render(context);

        files["Core/Service/JobService.cs"]
            .Should().Contain("IAppTelemetry").And.Contain("telemetry.MonitoredActionAsync")
            .And.NotContain("Observer");
    }
}
