using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Cli.Tool.Command;
using Anarchy.Toolkit.Build.Cli.Tool.Service;
using Anarchy.Toolkit.Build.Cli.Tool.Wizard;
using AwesomeAssertions;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Tool;

public class ToolkitToolTest
{
    private static readonly ToolkitPackageVersions Versions = new(new Dictionary<string, string> {
        ["Anarchy.Toolkit.Build.Fallout"] = "1.2.3",
        ["Anarchy.Toolkit.Web"] = "2.0.0",
        ["Anarchy.Toolkit.Web.Ui"] = "2.1.0",
        ["Anarchy.Toolkit.Hosting"] = "3.0.0"
    });

    private static string TempRoot() => Path.Combine(Path.GetTempPath(), "anarchy-tool-" + Guid.NewGuid());

    private static ToolApplication Application(ScriptedPrompter prompter, string? workingDirectory = null) =>
        new(prompter, Versions, workingDirectory ?? Path.GetTempPath());

    [Theory]
    [InlineData(new string[0], ToolMode.Help, false)]
    [InlineData(new[] { "-h" }, ToolMode.Help, false)]
    [InlineData(new[] { "bootstrap" }, ToolMode.Bootstrap, true)]
    [InlineData(new[] { "bootstrap", "MyApp" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--output", "dir" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--output" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "scaffold" }, ToolMode.Scaffold, true)]
    [InlineData(new[] { "scaffold", "API", "Orders" }, ToolMode.Scaffold, false)]
    [InlineData(new[] { "scaffold", "worker", "Orders" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "MyApp" }, ToolMode.Invalid, false)]
    public void CommandLine_SelectsMode(string[] args, ToolMode mode, bool interactive)
    {
        var command = ToolCommandLine.Parse(args);
        command.Mode.Should().Be(mode);
        if (mode != ToolMode.Invalid) command.IsInteractive.Should().Be(interactive);
    }

    [Fact]
    public void Layout_PlacesProjectsByFolderOrPrefix()
    {
        var api = new ApplicationSpec(ApplicationKind.Api, "Orders");
        var job = new ApplicationSpec(ApplicationKind.Job, "Reports");

        SolutionLayout.Default.ProjectPath(api).Should().Be("src/Api/Orders/Orders.csproj");
        SolutionLayout.Default.RootNamespace(job).Should().Be("Jobs.Reports");
        SolutionLayout.Default.LibraryProjectPath("Core").Should().Be("src/Library/Core/Core.csproj");
        SolutionLayout.Default.ToFalloutParameters()["LayeringApplicationFolders"]
            .Should().BeEquivalentTo(new[] { "src/Api=Web", "src/Jobs=Hosting", "src/Ui=Web" });

        var prefix = SolutionLayout.Default with { Mode = LayeringMode.Prefix };
        prefix.ProjectPath(job).Should().Be("src/Hosting.Job.Reports/Hosting.Job.Reports.csproj");
        prefix.LibraryProjectPath("Core").Should().Be("src/Core/Core.csproj");
        (prefix with { Layers = ["Core", "Web"] }).Supports(ApplicationKind.Job).Should().BeFalse();
    }

    [Fact]
    public void BootstrapWizard_ValidatesAnswersAndPlansApplicationsInKindOrder()
    {
        var root = TempRoot();
        var prompter = new ScriptedPrompter(
            "bad name", "MyApp", root,
            LayeringMode.Folder, "Core, Testing", "",
            new object[] { ApplicationKind.Ui, ApplicationKind.Api },
            "Orders", true, "orders", "Billing", false,
            "Billing", "Admin", false,
            true);

        var plan = new BootstrapWizard(prompter).Run();

        plan.Should().NotBeNull();
        plan!.Name.Should().Be("MyApp");
        plan.RootDirectory.Should().Be(Path.GetFullPath(root));
        plan.Layout.Should().BeEquivalentTo(SolutionLayout.Default);
        plan.Applications.Select(plan.Layout.ProjectPath).Should().Equal(
            "src/Api/Orders/Orders.csproj", "src/Api/Billing/Billing.csproj", "src/Ui/Admin/Admin.csproj");
        prompter.ValidationErrors.Should().HaveCount(4)
            .And.ContainMatch("Solution names*").And.ContainMatch("*reserved*").And.ContainMatch("*orders already exists*")
            .And.ContainMatch("*Billing already exists*");
        prompter.ShownPlan.Should().Be(plan);
        plan.Ports.Values.Should().Equal((5000, 5001), (5002, 5003), (5004, 5005));
    }

    [Fact]
    public void BootstrapWizard_DefaultsDirectoryToSolutionNameAndRejectsNonEmptyDirectories()
    {
        var existing = TempRoot();
        Directory.CreateDirectory(existing);
        File.WriteAllText(Path.Combine(existing, "keep"), "");
        try
        {
            var prompter = new ScriptedPrompter("MyApp", existing, "", LayeringMode.Prefix, "Core Web", Array.Empty<object>(), true);
            var plan = new BootstrapWizard(prompter).Run();
            plan!.RootDirectory.Should().Be(Path.GetFullPath("MyApp"));
            plan.Layout.Should().BeEquivalentTo(new SolutionLayout(LayeringMode.Prefix, ["Core", "Web"]));
            plan.Applications.Should().BeEmpty();
            prompter.ValidationErrors.Should().ContainSingle().Which.Should().Contain("empty");
        }
        finally
        {
            Directory.Delete(existing, true);
        }
    }

    [Fact]
    public void Run_WhenWizardIsDeclined_WritesNothing()
    {
        var root = TempRoot();
        var prompter = new ScriptedPrompter("MyApp", root, LayeringMode.Folder, "", Array.Empty<object>(), false);

        Application(prompter).Run(["bootstrap"], interactive: true).Should().Be(1);

        Directory.Exists(root).Should().BeFalse();
        prompter.Messages.Should().ContainMatch("Cancelled*");
    }

    [Fact]
    public void Run_RejectsWizardsWithoutInteractiveInputAndInvalidArguments()
    {
        var prompter = new ScriptedPrompter();
        var application = Application(prompter);

        application.Run([], interactive: false).Should().Be(0);
        application.Run(["bootstrap"], interactive: false).Should().Be(1);
        application.Run(["scaffold"], interactive: false).Should().Be(1);
        application.Run(["bootstrap", "../bad"], interactive: false).Should().Be(1);
        application.Run(["scaffold", "api", "Orders"], interactive: false).Should().Be(1);
        application.Run(["--help"], interactive: false).Should().Be(0);

        prompter.Errors.Should().ContainMatch("Interactive input is unavailable*").And.ContainMatch("No Anarchy solution*");
        prompter.ShowedHelp.Should().BeTrue();
    }

    [Fact]
    public void Run_GivenNonEmptyOutput_FailsWithoutTouchingIt()
    {
        var root = TempRoot();
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "existing"), "keep");
        try
        {
            var prompter = new ScriptedPrompter();
            Application(prompter).Run(["bootstrap", "MyApp", "--output", root], interactive: false).Should().Be(1);
            Directory.GetFileSystemEntries(root).Should().ContainSingle();
            prompter.Errors.Should().ContainMatch("*empty*");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_BootstrapsByFolderThenScaffoldsIntoTheSolution()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter(
                "MyApp", root, LayeringMode.Folder, "",
                new object[] { ApplicationKind.Api, ApplicationKind.Job },
                "Orders", false, "Billing", false,
                true);

            Application(prompter).Run(["bootstrap"], interactive: true).Should().Be(0, string.Join("\n", prompter.Errors));

            prompter.BannersShown.Should().Be(1);
            prompter.Progress.Should().Contain("Creating src/Api/Orders/Orders.csproj");
            var solution = File.ReadAllText(Path.Combine(root, "MyApp.sln"));
            solution.Should().Contain("_build.csproj").And.Contain("Configuration.csproj").And.Contain("Orders.csproj").And.Contain("Billing.csproj");
            File.Exists(Path.Combine(root, "src", "Core", "Core.csproj")).Should().BeFalse();
            File.ReadAllText(Path.Combine(root, "src", "Library", "Web", "Web.csproj")).Should().Contain(@"..\Hosting\Hosting.csproj");
            var packages = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
            packages.Should().Contain("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Build.Fallout\" Version=\"1.2.3\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Web\" Version=\"2.0.0\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Hosting\" Version=\"3.0.0\" />")
                .And.NotContain("Anarchy.Toolkit.Web.Ui");
            File.ReadAllText(Path.Combine(root, "build", "_build.csproj"))
                .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Build.Fallout\" />").And.NotContain("Version=");
            var api = File.ReadAllText(Path.Combine(root, "src", "Api", "Orders", "Orders.csproj"));
            api.Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Web\" />").And.NotContain("Version=").And.Contain("<IsPackable>false</IsPackable>")
                .And.Contain("<RootNamespace>Api.Orders</RootNamespace>").And.Contain(@"..\..\Library\Web\Web.csproj");
            File.ReadAllText(Path.Combine(root, "src", "Jobs", "Billing", "Billing.csproj"))
                .Should().Contain(@"..\..\Library\Hosting\Hosting.csproj").And.NotContain("<OutputType>");
            var parameters = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".fallout", "parameters.json")))!;
            parameters["Solution"]!.GetValue<string>().Should().Be("MyApp.sln");
            parameters["LayeringByFolder"]!.GetValue<bool>().Should().BeTrue();
            parameters["LayeringRoot"]!.GetValue<string>().Should().Be("src/Library");

            // Scaffold from a subdirectory: the solution is found upwards and its layout and ports are respected.
            var scaffold = new ScriptedPrompter(ApplicationKind.Ui, "Orders", "Admin", true);
            Application(scaffold, Path.Combine(root, "src", "Api")).Run(["scaffold"], interactive: true)
                .Should().Be(0, string.Join("\n", scaffold.Errors));

            scaffold.ValidationErrors.Should().ContainSingle().Which.Should().Contain("Orders already exists");
            File.ReadAllText(Path.Combine(root, "src", "Ui", "Admin", "Program.cs")).Should().Contain("args, 5002, 5003");
            File.ReadAllText(Path.Combine(root, "MyApp.sln")).Should().Contain("Admin.csproj");
            File.ReadAllText(Path.Combine(root, "src", "Ui", "Admin", "Admin.csproj")).Should().NotContain("Version=");
            var updated = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
            updated.Should().Contain("    <PackageVersion Include=\"Anarchy.Toolkit.Web.Ui\" Version=\"2.1.0\" />\n");
            updated.Split("Include=\"Anarchy.Toolkit.Web\"").Should().HaveCount(2);

            var direct = new ScriptedPrompter();
            Application(direct, root).Run(["scaffold", "api", "Admin"], interactive: false).Should().Be(1);
            direct.Errors.Should().ContainMatch("*Admin already exists*");
            Application(direct, root).Run(["scaffold", "job", "Exports"], interactive: false).Should().Be(0);
            SolutionWorkspace.Load(root).ProjectNames.Should().Contain("Exports");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_BootstrapsBaselineByPrefixWhenAsked()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter("MyApp", root, LayeringMode.Prefix, "Core, Web",
                new object[] { ApplicationKind.Api }, "Orders", false, true);

            Application(prompter).Run(["bootstrap"], interactive: true).Should().Be(0, string.Join("\n", prompter.Errors));

            File.ReadAllText(Path.Combine(root, "src", "Web.Api.Orders", "Web.Api.Orders.csproj")).Should().Contain(@"..\Web\Web.csproj");
            File.Exists(Path.Combine(root, "src", "Core", "Core.csproj")).Should().BeTrue();
            var workspace = SolutionWorkspace.Load(root);
            workspace.Layout.Should().BeEquivalentTo(new SolutionLayout(LayeringMode.Prefix, ["Core", "Web"]));
            workspace.UsedPorts.Should().BeEquivalentTo(new[] { 5000, 5001 });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_ScaffoldsVersionedReferencesWhenTheSolutionManagesNoVersionsCentrally()
    {
        var root = TempRoot();
        try
        {
            Application(new ScriptedPrompter()).Run(["bootstrap", "MyApp", "--output", root], interactive: false).Should().Be(0);
            File.Delete(Path.Combine(root, "Directory.Packages.props"));

            var prompter = new ScriptedPrompter();
            Application(prompter, root).Run(["scaffold", "api", "Orders"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));

            File.ReadAllText(Path.Combine(root, "src", "Api", "Orders", "Orders.csproj"))
                .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Web\" Version=\"2.0.0\" />");
            File.Exists(Path.Combine(root, "Directory.Packages.props")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
