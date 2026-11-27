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

    [Theory]
    [InlineData(ApplicationKind.Api, "OrdersStartup")]
    [InlineData(ApplicationKind.Ui, "OrdersStartup")]
    [InlineData(ApplicationKind.Job, "OrdersStartup")]
    public void Render_InFolderMode_UsesApplicationSpecificStartup(ApplicationKind kind, string startup)
    {
        var context = new ApplicationContext(new ApplicationSpec(kind, "Orders"), "Orders", "App.Orders", null, null, null) {
            LayeringMode = LayeringMode.Folder
        };

        var files = ApplicationTemplates.Render(context);

        files.Should().ContainKey($"Composition/{startup}.cs");
        files["EntryPointLocal.cs"].Should().Contain(startup);
        files["Orders.csproj"].Should().Contain("<StartupObject>App.Orders.EntryPointLocal</StartupObject>");
    }

    [Fact]
    public void RenderUi_QualifiesTheBlazorAppTypeWhenTheRootNamespaceIsApp()
    {
        var context = new ApplicationContext(new ApplicationSpec(ApplicationKind.Ui, "Admin"), "Admin", "App.Admin", null, null, null) {
            LayeringMode = LayeringMode.Folder
        };

        ApplicationTemplates.Render(context)["Composition/AdminStartup.cs"]
            .Should().Contain("MapRazorComponents<global::App.Admin.Ui.Template.App>()");
    }

    [Fact]
    public void RenderWeb_OnlyGeneratesChosenOptionalHosts()
    {
        var app = new ApplicationSpec(ApplicationKind.Api, "Orders") { Container = true, ApiLambda = true };
        var context = new ApplicationContext(app, "Orders", "App.Orders", null, null, null);

        var files = ApplicationTemplates.Render(context);

        files.Should().ContainKeys("EntryPointLocal.cs", "EntryPointContainer.cs", "EntryPointLambda.cs", "Composition/Startup.cs");
        files["EntryPointLocal.cs"].Should().Contain("RUNTIME_HOST");
        files["EntryPointLambda.cs"].Should().Contain("AwsWebAppEntryPointBase<Startup>").And.Contain("using Anarchy.Toolkit.Runtime.Web.Aws;");
        ApplicationTemplates.Render(context with { App = app with { Container = false, ApiLambda = false } })
            .Should().NotContainKey("EntryPointContainer.cs").And.NotContainKey("EntryPointLambda.cs");
    }

    [Fact]
    public void RenderApi_EnablesSwaggerUnlessLeftOut()
    {
        var app = new ApplicationSpec(ApplicationKind.Api, "Orders");
        var context = new ApplicationContext(app, "Orders", "App.Orders", "2.0.0", null, null) { SwaggerPackageVersion = "2.3.0" };

        var files = ApplicationTemplates.Render(context);
        var unthemed = ApplicationTemplates.Render(context with { App = app with { SwaggerTheme = false } });
        var bare = ApplicationTemplates.Render(context with { App = app with { Swagger = false } });

        files["Orders.csproj"].Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Runtime.Web.Swagger\" Version=\"2.3.0\" />");
        files["Composition/Startup.cs"].Should().Contain(".AddToolkitSwagger(ApiName, AppSettings, Environment);")
            .And.NotContain("SwaggerThemeOptions");
        unthemed["Composition/Startup.cs"].Should().Contain("using Anarchy.Toolkit.Runtime.Web.Swagger.Entity;")
            .And.Contain(".Configure<SwaggerThemeOptions>(theme => theme.Enabled = false);");
        bare["Orders.csproj"].Should().NotContain("Swagger");
        bare["Composition/Startup.cs"].Should().NotContain("Swagger");
    }

    [Theory]
    [InlineData(JobExecutionMode.Worker)]
    [InlineData(JobExecutionMode.Once)]
    public void RenderJob_ProvidesLocalExecutionAndOptionalScheduledLambda(JobExecutionMode mode)
    {
        var app = new ApplicationSpec(ApplicationKind.Job, "Orders") { JobMode = mode, EventLambda = true };
        var files = ApplicationTemplates.Render(new ApplicationContext(app, "Orders", "Jobs.Orders", null, null, null));

        files.Should().ContainKeys("EntryPointLocal.cs", "EntryPointLambda.cs");
        files["EntryPointLocal.cs"].Should().Contain(mode == JobExecutionMode.Worker ? "RunAsync()" : "job.RunAsync");
        files["EntryPointLambda.cs"].Should().Contain("LambdaJobEntryPointBase<JobStartup>").And.Contain("BeginInvocation()");
        files.ContainsKey("Core/Service/JobWorker.cs").Should().Be(mode == JobExecutionMode.Worker);
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
