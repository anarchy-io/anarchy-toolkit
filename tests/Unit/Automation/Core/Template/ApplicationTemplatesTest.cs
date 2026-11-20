using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Template;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Template;

public class ApplicationTemplatesTest
{
    [Fact]
    public void RenderApi_UsesApplicationTelemetryInThePingController()
    {
        var context = new ApplicationContext(new ApplicationSpec(ApplicationKind.Api, "Orders"), "Orders.Api", "Orders.Api",
            null, (5000, 5001), null);

        var files = ApplicationTemplates.Render(context);

        files["Presentation/Controller/PingController.cs"]
            .Should().Contain("AppTelemetry telemetry").And.Contain("telemetry.LogInformation(this.CalledFrom()")
            .And.NotContain("Observer");
    }

    [Fact]
    public void RenderJob_UsesApplicationTelemetryForMonitoredWork()
    {
        var context = new ApplicationContext(new ApplicationSpec(ApplicationKind.Job, "Processor"), "Processor.Job", "Processor.Job",
            null, null, null);

        var files = ApplicationTemplates.Render(context);

        files["Core/Service/JobService.cs"]
            .Should().Contain("AppTelemetry telemetry").And.Contain("telemetry.MonitoredActionAsync")
            .And.NotContain("Observer");
    }
}
