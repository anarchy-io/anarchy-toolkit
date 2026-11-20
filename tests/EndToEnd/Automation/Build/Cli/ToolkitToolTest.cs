using Anarchy.Toolkit.Automation.Build.Cli.Command;
using Anarchy.Toolkit.Automation.Build.Cli.Service;
using Anarchy.Toolkit.Automation.Build.Cli.Wizard;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Tests.EndToEnd.Automation.Build.Cli.Fixture;
using AwesomeAssertions;
using Spectre.Console;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.EndToEnd.Automation.Build.Cli;

public class ToolkitToolTest(ToolkitSolutionFixture fixture)
{
    private static ToolkitPackageVersions Versions => ToolkitSolutionFixture.Versions;

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
    [InlineData(new[] { "bootstrap", "MyApp", "--with-skills", "--output", "dir" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--with-skills", "--with-skills" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layering", "folder" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layering", "prefix" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layout", "monorepo" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layout", "single-app", "--application-folder", "Shop" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "scaffold", "api", "Orders", "--layout", "single-app" }, ToolMode.Scaffold, false)]
    [InlineData(new[] { "scaffold", "--layout", "single-app" }, ToolMode.Scaffold, true)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layout", "other" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--application-folder", "Shop" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layering", "other" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layering" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layering", "folder", "--layering", "prefix" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layering-root", "code", "--save-preferences" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "--with-skills" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "skills", "install" }, ToolMode.Skills, false)]
    [InlineData(new[] { "skills", "install", "--output", "dir" }, ToolMode.Skills, false)]
    [InlineData(new[] { "skills" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "skills", "remove" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "scaffold" }, ToolMode.Scaffold, true)]
    [InlineData(new[] { "scaffold", "API", "Orders" }, ToolMode.Scaffold, false)]
    [InlineData(new[] { "scaffold", "worker", "Orders" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "scaffold", "api", "Orders", "--no-swagger-theme" }, ToolMode.Scaffold, false)]
    [InlineData(new[] { "scaffold", "api", "Orders", "--swagger-theme" }, ToolMode.Scaffold, false)]
    [InlineData(new[] { "scaffold", "job", "Orders", "--no-swagger-theme" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "scaffold", "api", "Orders", "--unknown" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "boilerplate", "list" }, ToolMode.Boilerplate, false)]
    [InlineData(new[] { "boilerplate", "--help" }, ToolMode.Help, false)]
    [InlineData(new[] { "boilerplate", "-h" }, ToolMode.Help, false)]
    [InlineData(new[] { "boilerplate", "help" }, ToolMode.Help, false)]
    [InlineData(new[] { "boilerplate", "show", "Makefile" }, ToolMode.Boilerplate, false)]
    [InlineData(new[] { "boilerplate", "show" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "boilerplate", "diff" }, ToolMode.Boilerplate, false)]
    [InlineData(new[] { "boilerplate", "diff", "Makefile", "scripts" }, ToolMode.Boilerplate, false)]
    [InlineData(new[] { "boilerplate", "diff", "--all" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "config", "show" }, ToolMode.Config, false)]
    [InlineData(new[] { "config", "set", "docker.timeout", "45" }, ToolMode.Config, false)]
    [InlineData(new[] { "config", "unset", "docker.timeout" }, ToolMode.Config, false)]
    [InlineData(new[] { "config", "configure" }, ToolMode.Config, true)]
    [InlineData(new[] { "config", "set", "docker.timeout" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "config", "other" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "MyApp" }, ToolMode.Invalid, false)]
    public void CommandLine_SelectsMode(string[] args, ToolMode mode, bool interactive)
    {
        var command = ToolCommandLine.Parse(args);
        command.Mode.Should().Be(mode);
        if (mode != ToolMode.Invalid) command.IsInteractive.Should().Be(interactive);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Boilerplate_PrintsPlainOutputWithoutTheBanner(string newLine)
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var prompter = new ScriptedPrompter();
            string Run(int expectedExitCode, params string[] args)
            {
                var output = new StringWriter { NewLine = newLine };
                new ToolApplication(prompter, Versions, root, output).Run(args, interactive: false).Should().Be(expectedExitCode);
                return output.ToString();
            }

            Run(0, "boilerplate", "list").Should().Contain($"Makefile{newLine}").And.Contain("scripts/build.anarchy.sh");
            Run(0, "boilerplate", "show", "Makefile").Should().Be(BoilerplateCatalog.Read("Makefile"));
            Run(1, "boilerplate", "diff", "Makefile").Should().Be($"missing: Makefile{newLine}");
            SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = "Makefile" });
            Run(0, "boilerplate", "diff", "Makefile").Should().Be($"identical: Makefile{newLine}");
            Run(1, "boilerplate", "show", "nope").Should().BeEmpty();
            prompter.Errors.Should().ContainSingle().Which.Should().Contain("nope");
            prompter.BannersShown.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData(InteractionSupport.No, false)]
    [InlineData(InteractionSupport.Yes, true)]
    public void BoilerplateHelp_WritesReadableHelpForInteractiveAndRedirectedOutput(
        InteractionSupport interaction,
        bool interactive)
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings {
            Out = new AnsiConsoleOutput(output),
            Ansi = AnsiSupport.No,
            Interactive = interaction,
            Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
        });
        var application = new ToolApplication(new SpectreWizardPrompter(console), Versions, TempRoot(), output);

        application.Run(["boilerplate", "--help"], interactive).Should().Be(0);

        output.ToString().Should().Contain("boilerplate list").And.Contain("boilerplate diff")
            .And.NotContain("\u001b[");
    }

    [Fact]
    public void Layout_PlacesProjectsByFolderOrPrefix()
    {
        var api = new ApplicationSpec(ApplicationKind.Api, "Orders");
        var job = new ApplicationSpec(ApplicationKind.Job, "Reports");

        SolutionLayout.Default.ProjectPath(api).Should().Be("src/Api/Orders/Orders.csproj");
        SolutionLayout.Default.RootNamespace(job).Should().Be("Jobs.Reports");
        SolutionLayout.Default.LibraryProjectPath("Core").Should().Be("src/Library/Core/Core.csproj");
        SolutionLayout.Default.LibraryRoot.Should().Be("src/Library");
        SolutionLayout.Default.ApplicationFolders.Should().Equal(new Dictionary<string, string> {
            ["src/Api"] = "Web",
            ["src/Jobs"] = "Hosting",
            ["src/Ui"] = "Web"
        });
        new SolutionLayout(LayeringMode.Folder, ["Core", "Web"]).ApplicationFolders.Keys.Should().Equal("src/Api", "src/Ui");

        var prefix = new SolutionLayout(LayeringMode.Prefix, SolutionLayout.DefaultLayers);
        prefix.LibraryRoot.Should().BeNull();
        prefix.ApplicationFolders.Should().BeEmpty();
        prefix.ProjectPath(job).Should().Be("src/Hosting.Job.Reports/Hosting.Job.Reports.csproj");
        prefix.LibraryProjectPath("Core").Should().Be("src/Core/Core.csproj");
        (prefix with { Layers = ["Core", "Web"] }).Supports(ApplicationKind.Job).Should().BeFalse();
    }

    [Fact]
    public void SingleAppPreset_UsesTheApplicationLayerAndAcceptsOneCustomFolder()
    {
        var api = SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Api, "Shop");
        api.Root.Should().Be("src");
        api.LibraryRoot.Should().Be("src");
        api.Layers.Should().Equal("Core", "Infrastructure", "Configuration", "Web");
        api.ApplicationFolders.Should().Equal(new Dictionary<string, string> { ["src/Shop"] = "Web" });
        api.ProjectPath(new ApplicationSpec(ApplicationKind.Api, "Store")).Should().Be("src/Shop/Store/Store.csproj");
        LayeringConfiguration.Validate(api).Should().BeEmpty();

        var job = SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Job);
        job.Layers.Should().Equal("Core", "Infrastructure", "Configuration", "Hosting");
        job.ProjectPath(new ApplicationSpec(ApplicationKind.Job, "Worker")).Should().Be("src/App/Worker/Worker.csproj");
        LayeringConfiguration.Validate(job).Should().BeEmpty();

        SolutionLayoutPresets.Create(SolutionLayoutPreset.Monorepo).Should().BeEquivalentTo(SolutionLayout.Default);
    }

    [Fact]
    public void BootstrapWizard_ValidatesAnswersAndPlansApplicationsInKindOrder()
    {
        var root = TempRoot();
        var prompter = new ScriptedPrompter(
            "bad name", "MyApp", root,
            false,
            LayeringMode.Folder, "Core, Testing", "",
            new object[] { ApplicationKind.Ui, ApplicationKind.Api },
            "Orders", true, "orders", "Billing", false,
            "Billing", "Admin", false,
            false, false, true);

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
            var prompter = new ScriptedPrompter("MyApp", existing, "", false, LayeringMode.Prefix, "Core Web", Array.Empty<object>(), false, false, true);
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
    public void BootstrapWizard_ChoosesSingleAppBeforeLayeringAndUsesItsPreset()
    {
        var root = TempRoot();
        var prompter = new ScriptedPrompter(
            "Shop", root, true, ApplicationKind.Api, "Shop", LayeringMode.Folder, "", "Orders", false, false, true);

        var plan = new BootstrapWizard(prompter).Run();

        plan.Should().NotBeNull();
        plan!.Layout.Should().BeEquivalentTo(SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Api, "Shop"));
        plan.Applications.Select(plan.Layout.ProjectPath).Should().Equal("src/Shop/Orders/Orders.csproj");
        plan.Layout.Layers.Should().Equal("Core", "Infrastructure", "Configuration", "Web");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_InteractiveSingleAppBootstrapAndScaffoldRespectPreferenceConsent(bool savePreferences)
    {
        var root = TempRoot();
        try
        {
            var bootstrap = new ScriptedPrompter(
                "Shop", root, true, ApplicationKind.Api, "Shop", LayeringMode.Folder, "", "Orders", false, savePreferences, true);
            Application(bootstrap).Run(["bootstrap"], interactive: true).Should().Be(0, string.Join("\n", bootstrap.Errors));
            File.Exists(Path.Combine(root, "src", "Shop", "Orders", "Orders.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().Be(savePreferences);
            if (savePreferences)
                AutomationPreferences.Read(root).ResolveLayout().Should().BeEquivalentTo(
                    SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Api, "Shop"));

            var scaffold = new ScriptedPrompter(ApplicationKind.Ui, "Admin", true);
            Application(scaffold, root).Run(["scaffold", "--layout", "single-app", "--application-folder", "Shop"], interactive: true)
                .Should().Be(0, string.Join("\n", scaffold.Errors));
            File.Exists(Path.Combine(root, "src", "Shop", "Admin", "Admin.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().Be(savePreferences);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_WhenWizardIsDeclined_WritesNothing()
    {
        var root = TempRoot();
        var prompter = new ScriptedPrompter("MyApp", root, false, LayeringMode.Folder, "", Array.Empty<object>(), false, false, false);

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
        application.Run(["boilerplate", "--help"], interactive: false).Should().Be(0);

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
                "MyApp", root, false, LayeringMode.Folder, "",
                new object[] { ApplicationKind.Api, ApplicationKind.Job },
                "Orders", false, "Billing", false,
                false, false, true);

            Application(prompter).Run(["bootstrap"], interactive: true).Should().Be(0, string.Join("\n", prompter.Errors));

            prompter.BannersShown.Should().Be(1);
            prompter.Progress.Should().Contain("Creating src/Api/Orders/Orders.csproj");
            var solution = File.ReadAllText(Path.Combine(root, "MyApp.sln"));
            solution.Should().Contain("_build.csproj").And.Contain("Configuration.csproj").And.Contain("Orders.csproj").And.Contain("Billing.csproj");
            File.Exists(Path.Combine(root, "src", "Core", "Core.csproj")).Should().BeFalse();
            Directory.Exists(Path.Combine(root, ".agents")).Should().BeFalse();
            File.ReadAllText(Path.Combine(root, "src", "Library", "Web", "Web.csproj")).Should().Contain(@"..\Hosting\Hosting.csproj");
            var packages = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
            packages.Should().Contain("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Automation.Build\" Version=\"1.2.3\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Web\" Version=\"2.0.0\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Hosting\" Version=\"3.0.0\" />")
                .And.NotContain("Anarchy.Toolkit.Web.Ui");
            File.ReadAllText(Path.Combine(root, "build", "_build.csproj"))
                .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Automation.Build\" />").And.NotContain("Version=");
            File.ReadAllText(Path.Combine(root, "build", "Program.cs"))
                .Should().Contain("class Build : AnarchyBuild,").And.Contain("using Anarchy.Toolkit.Automation.Build.Component;")
                .And.NotContain("using Fallout.Common;").And.NotContain(": FalloutBuild");
            var api = File.ReadAllText(Path.Combine(root, "src", "Api", "Orders", "Orders.csproj"));
            api.Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Web\" />").And.NotContain("Version=").And.Contain("<IsPackable>false</IsPackable>")
                .And.Contain("<RootNamespace>Api.Orders</RootNamespace>").And.Contain(@"..\..\Library\Web\Web.csproj");
            File.ReadAllText(Path.Combine(root, "src", "Jobs", "Billing", "Billing.csproj"))
                .Should().Contain(@"..\..\Library\Hosting\Hosting.csproj").And.NotContain("<OutputType>");
            var parameters = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".fallout", "parameters.json")))!.AsObject();
            parameters["Solution"]!.GetValue<string>().Should().Be("MyApp.sln");
            parameters.Select(parameter => parameter.Key).Should().NotContain(key => key.StartsWith("Layering"));
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
            prompter.Confirmations.Should().Contain(entry => entry.Prompt.Contains("Save the selected layering preferences") && !entry.DefaultValue);

            // Scaffold from a subdirectory: the solution is found upwards and its layout and ports are respected.
            var scaffold = new ScriptedPrompter(ApplicationKind.Ui, "Orders", "Admin", true);
            Application(scaffold, Path.Combine(root, "src", "Api")).Run(["scaffold"], interactive: true)
                .Should().Be(0, string.Join("\n", scaffold.Errors));

            scaffold.ValidationErrors.Should().ContainSingle().Which.Should().Contain("Orders already exists");
            File.ReadAllText(Path.Combine(root, "src", "Ui", "Admin", "Program.cs")).Should().Contain("args, 5002, 5003");
            File.ReadAllText(Path.Combine(root, "MyApp.sln")).Should().Contain("Admin.csproj");
            File.ReadAllText(Path.Combine(root, "src", "Ui", "Admin", "Admin.csproj")).Should().NotContain("Version=");
            var updated = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
            File.ReadAllLines(Path.Combine(root, "Directory.Packages.props"))
                .Should().Contain("    <PackageVersion Include=\"Anarchy.Toolkit.Web.Ui\" Version=\"2.1.0\" />");
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
            var prompter = new ScriptedPrompter("MyApp", root, false, LayeringMode.Prefix, "Core, Web",
                new object[] { ApplicationKind.Api }, "Orders", false, true, true, true);

            Application(prompter).Run(["bootstrap"], interactive: true).Should().Be(0, string.Join("\n", prompter.Errors));

            File.ReadAllText(Path.Combine(root, "src", "Web.Api.Orders", "Web.Api.Orders.csproj")).Should().Contain(@"..\Web\Web.csproj");
            File.Exists(Path.Combine(root, "src", "Core", "Core.csproj")).Should().BeTrue();
            var preferences = AutomationPreferences.Read(root);
            preferences.Saved["layering"]!.AsObject().Select(property => property.Key)
                .Should().Equal("mode", "layers");
            preferences.Saved["layering"]!["mode"]!.GetValue<string>().Should().Be("prefix");
            preferences.Saved["layering"]!["layers"]!.AsArray().Select(layer => layer!.GetValue<string>())
                .Should().Equal("Core", "Web");
            var workspace = SolutionWorkspace.Load(root);
            workspace.Layout.Should().BeEquivalentTo(new SolutionLayout(LayeringMode.Prefix, ["Core", "Web"]));
            workspace.UsedPorts.Should().BeEquivalentTo(new[] { 5000, 5001 });
            File.Exists(Path.Combine(root, ".agents", "skills", "anarchy", "SKILL.md")).Should().BeTrue();
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
            fixture.CopyTo(root);
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

    [Fact]
    public void Run_ScaffoldsAnApiWithOrWithoutTheSwaggerTheme()
    {
        var root = TempRoot();
        try
        {
            fixture.CopyTo(root);

            var prompter = new ScriptedPrompter();
            Application(prompter, root).Run(["scaffold", "api", "Orders"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "api", "Billing", "--no-swagger-theme"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));

            File.ReadAllText(Path.Combine(root, "src", "Api", "Orders", "Composition", "Startup.cs"))
                .Should().NotContain("SwaggerThemeOptions");
            File.ReadAllText(Path.Combine(root, "src", "Api", "Billing", "Composition", "Startup.cs"))
                .Should().Contain("using Anarchy.Toolkit.Web.Entity.Swagger;")
                .And.Contain("            .Configure<SwaggerThemeOptions>(theme => theme.Enabled = false);");
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_ScaffoldSwaggerThemeUsesSavedPreferenceUnlessExplicitlyOverridden()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            fixture.CopyTo(root);

            Application(prompter, root).Run(["config", "set", "scaffolding.swaggerTheme", "false"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "api", "Orders"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "api", "Billing", "--swagger-theme"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));

            File.ReadAllText(Path.Combine(root, "src", "Api", "Orders", "Composition", "Startup.cs"))
                .Should().Contain("theme.Enabled = false");
            File.ReadAllText(Path.Combine(root, "src", "Api", "Billing", "Composition", "Startup.cs"))
                .Should().NotContain("theme.Enabled = false");

            Application(prompter, root).Run(["config", "set", "scaffolding.swaggerTheme", "true"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "api", "Claims", "--no-swagger-theme"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            File.ReadAllText(Path.Combine(root, "src", "Api", "Claims", "Composition", "Startup.cs"))
                .Should().Contain("theme.Enabled = false");
            AutomationPreferences.Read(root).Get<bool?>("scaffolding.swaggerTheme").Should().BeTrue();

            Application(prompter, root).Run(["scaffold", "api", "Reports", "--no-swagger-theme", "--save-preferences"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            AutomationPreferences.Read(root).Get<bool?>("scaffolding.swaggerTheme").Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_ScaffoldWizardSavesItsSwaggerChoiceOnlyAfterOptIn()
    {
        var root = TempRoot();
        try
        {
            fixture.CopyTo(root);

            var declined = new ScriptedPrompter(ApplicationKind.Api, "Orders", false, true, false);
            Application(declined, root).Run(["scaffold"], interactive: true).Should().Be(0, string.Join("\n", declined.Errors));
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
            declined.Confirmations.Last().DefaultValue.Should().BeFalse();

            var accepted = new ScriptedPrompter(ApplicationKind.Api, "Billing", true, true, true);
            Application(accepted, root).Run(["scaffold"], interactive: true).Should().Be(0, string.Join("\n", accepted.Errors));
            accepted.Confirmations.Last().DefaultValue.Should().BeFalse();
            AutomationPreferences.Read(root).Get<bool?>("scaffolding.swaggerTheme").Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CommandLine_ReadsBootstrapSkillsAndOutput()
    {
        var command = ToolCommandLine.Parse(["bootstrap", "MyApp", "-o", "dir", "--with-skills"]);
        command.Should().BeEquivalentTo(new ToolCommand(ToolMode.Bootstrap, "MyApp", "dir") { AgentSkills = true });
        ToolCommandLine.Parse(["bootstrap", "MyApp"]).AgentSkills.Should().BeFalse();
    }

    [Fact]
    public void CommandLine_PreservesExplicitLayoutAndThemeOptionsAndPreferenceSaving()
    {
        var bootstrap = ToolCommandLine.Parse([
            "bootstrap", "MyApp", "--layering-root", "code", "--layering-layers", "[\"Core\",\"Web\"]",
            "--layering-application-folders", "{\"code/Api\":\"Web\"}", "--save-preferences"
        ]);
        bootstrap.SavePreferences.Should().BeTrue();
        bootstrap.LayeringOverrides!["root"]!.GetValue<string>().Should().Be("code");
        bootstrap.LayeringOverrides["layers"]!.AsArray().Select(layer => layer!.GetValue<string>()).Should().Equal("Core", "Web");
        bootstrap.LayeringOverrides["applicationFolders"]!["code/Api"]!.GetValue<string>().Should().Be("Web");

        ToolCommandLine.Parse(["scaffold", "api", "Orders", "--no-swagger-theme"]).SwaggerThemeOverride.Should().BeFalse();
        ToolCommandLine.Parse(["scaffold", "api", "Orders", "--swagger-theme"]).SwaggerThemeOverride.Should().BeTrue();
        ToolCommandLine.Parse(["scaffold", "api", "Orders", "--save-preferences"]).SavePreferences.Should().BeTrue();
        var config = ToolCommandLine.Parse(["config", "set", "docker.waitFor", "[]", "--output", "solution"]);
        config.ConfigAction.Should().Be("set");
        config.PreferenceKey.Should().Be("docker.waitFor");
        config.PreferenceValue.Should().Be("[]");
        config.OutputDirectory.Should().Be("solution");
    }

    [Theory]
    [InlineData("folder", LayeringMode.Folder)]
    [InlineData("prefix", LayeringMode.Prefix)]
    public void Run_BootstrapsAndScaffoldsUsingTheSelectedLayeringMode(string mode, LayeringMode expected)
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            Application(prompter).Run(["bootstrap", "MyApp", "--output", root, "--layering", mode, "--save-preferences"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            SolutionWorkspace.Load(root).Layout.Mode.Should().Be(expected);

            Application(prompter, root).Run(["scaffold", "api", "Orders"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            var layout = new SolutionLayout(expected, SolutionLayout.DefaultLayers);
            File.Exists(Path.Combine(root, layout.ProjectPath(new ApplicationSpec(ApplicationKind.Api, "Orders"))))
                .Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData("monorepo", false)]
    [InlineData("monorepo", true)]
    [InlineData("single-app", false)]
    [InlineData("single-app", true)]
    public void Run_BootstrapsAndScaffoldsWithEachPresetAndOptionalSavedPreferences(string preset, bool savePreferences)
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            var bootstrapArguments = new List<string> { "bootstrap", "Shop", "--output", root, "--layout", preset };
            if (preset == "single-app") bootstrapArguments.AddRange(["--application-folder", "Shop"]);
            if (savePreferences) bootstrapArguments.Add("--save-preferences");
            Application(prompter).Run(bootstrapArguments, interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            var expectedLayout = preset == "single-app"
                ? SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Api, "Shop")
                : SolutionLayout.Default;
            var resolvedAfterBootstrap = SolutionWorkspace.Load(root).Layout;
            if (savePreferences) resolvedAfterBootstrap.Should().BeEquivalentTo(expectedLayout);
            else resolvedAfterBootstrap.Should().BeEquivalentTo(SolutionLayout.Default);
            File.Exists(Path.Combine(root, preset == "single-app" ? "src/Core/Core.csproj" : "src/Library/Core/Core.csproj"))
                .Should().BeTrue();

            var scaffoldArguments = new List<string> { "scaffold", "api", "Orders", "--layout", preset };
            if (preset == "single-app") scaffoldArguments.AddRange(["--application-folder", "Shop"]);
            if (savePreferences) scaffoldArguments.Add("--save-preferences");
            Application(prompter, root).Run(scaffoldArguments, interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            File.Exists(Path.Combine(root, preset == "single-app" ? "src/Shop/Orders/Orders.csproj" : "src/Api/Orders/Orders.csproj"))
                .Should().BeTrue();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().Be(savePreferences);
            if (savePreferences)
                AutomationPreferences.Read(root).ResolveLayout().Should().BeEquivalentTo(expectedLayout);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_SingleAppPresetSupportsCustomRootAndApplicationMapping()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            Application(prompter).Run([
                "bootstrap", "Shop", "--output", root, "--layout", "single-app", "--application-folder", "Cli",
                "--layering-root", "code", "--save-preferences"
            ], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            var expected = new SolutionLayout(LayeringMode.Folder, ["Core", "Infrastructure", "Configuration", "Web"]) {
                Root = "code",
                LibraryRoot = "code",
                ApplicationFolders = new Dictionary<string, string> { ["code/Cli"] = "Web" }
            };
            SolutionWorkspace.Load(root).Layout.Should().BeEquivalentTo(expected);
            var build = Path.Combine(root, "build", "Program.cs");
            File.ReadAllText(build).Should().Contain("ILayeringBuild");

            Application(prompter, root).Run([
                "scaffold", "api", "Terminal", "--layout", "single-app", "--application-folder", "Cli",
                "--layering-root", "code"
            ], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));
            File.Exists(Path.Combine(root, "code", "Cli", "Terminal", "Terminal.csproj")).Should().BeTrue();
            LayeringConfiguration.Validate(SolutionWorkspace.Load(root).Layout).Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_BootstrapsWithSavePreferencesPersistsOnlyExplicitLayoutFields()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            Application(prompter).Run([
                "bootstrap", "MyApp", "--output", root, "--layering-root", "code", "--save-preferences"
            ], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            var preferences = AutomationPreferences.Read(root);
            preferences.Saved["layering"]!.AsObject().Select(property => property.Key).Should().Equal("root");
            preferences.Saved["layering"]!["root"]!.GetValue<string>().Should().Be("code");
            preferences.ResolveLayout().LibraryRoot.Should().Be("code/Library");
            File.Exists(Path.Combine(root, "code", "Library", "Core", "Core.csproj")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_GivenUnsavedNonDefaultLayout_SuggestsCanonicalBuildCommandForBootstrapAndScaffold()
    {
        var root = TempRoot();
        try
        {
            var bootstrap = new ScriptedPrompter();
            Application(bootstrap).Run([
                "bootstrap", "MyApp", "--output", root, "--layering", "prefix", "--layering-layers", "[\"Core\",\"Web\"]"
            ], interactive: false).Should().Be(0, string.Join("\n", bootstrap.Errors));

            var scaffold = new ScriptedPrompter();
            Application(scaffold, root).Run([
                "scaffold", "api", "Orders", "--layering", "prefix", "--layering-layers", "[\"Core\",\"Web\"]"
            ], interactive: false).Should().Be(0, string.Join("\n", scaffold.Errors));

            var log = Path.Combine(root, "suggested-command.log");
            if (!OperatingSystem.IsWindows())
                File.WriteAllText(Path.Combine(root, "scripts", "build.anarchy.sh"), "#!/usr/bin/env bash\nprintf '<%s>\\n' \"$@\" > \"$MAKE_TEST_LOG\"\n");
            foreach (var prompter in new[] { bootstrap, scaffold })
            {
                var suggestion = prompter.NextSteps.Should().ContainSingle(step => step.StartsWith("make anarchy TARGET='CheckLayering "))
                    .Which;
                suggestion.Should().Contain("--LayeringMode").And.Contain("--LayeringLayers")
                    .And.Contain("--LayeringRoot").And.EndWith("'");
                prompter.NextSteps.Should().NotContain(step => step.StartsWith("make check_layering "));
                prompter.NextSteps.Should().Contain(step => step.StartsWith("anarchy-toolkit scaffold api NewApi "));
                if (!OperatingSystem.IsWindows())
                {
                    var result = LocalProcessTasks.Run(root, "/bin/bash", ["-c", suggestion], new Dictionary<string, string> {
                        ["MAKE_TEST_LOG"] = log,
                        ["ANARCHY_RELOAD_ENV"] = "false"
                    });
                    result.ExitCode.Should().Be(0, result.Output + result.Error);
                    File.ReadAllText(log).Should().Contain("<CheckLayering>\n<--LayeringMode>\n<prefix>\n")
                        .And.Contain("<--LayeringLayers>\n<Core>\n<Web>\n").And.Contain("<--LayeringRoot>\n<src>\n");
                }
            }
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_GivenUnsavedFolderLayout_SuggestedBuildCommandPreservesJsonMappings()
    {
        if (OperatingSystem.IsWindows()) return;
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            Application(prompter).Run([
                "bootstrap", "MyApp", "--output", root, "--layering-library-root", "src"
            ], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            var suggestion = prompter.NextSteps.Should().ContainSingle(step => step.StartsWith("make anarchy TARGET='CheckLayering "))
                .Which;
            var log = Path.Combine(root, "suggested-command.log");
            File.WriteAllText(Path.Combine(root, "scripts", "build.anarchy.sh"), "#!/usr/bin/env bash\nprintf '%s\\n' \"$@\" > \"$MAKE_TEST_LOG\"\n");
            var result = LocalProcessTasks.Run(root, "/bin/bash", ["-c", suggestion], new Dictionary<string, string> {
                ["MAKE_TEST_LOG"] = log,
                ["ANARCHY_RELOAD_ENV"] = "false"
            });
            result.ExitCode.Should().Be(0, result.Output + result.Error);
            var arguments = File.ReadAllLines(log);
            var index = Array.IndexOf(arguments, "--LayeringApplicationFolders");
            index.Should().BeGreaterThanOrEqualTo(0);
            index.Should().BeLessThan(arguments.Length - 1);
            var folders = JsonNode.Parse(arguments[index + 1])!.AsObject();
            folders.ToDictionary(entry => entry.Key, entry => entry.Value!.GetValue<string>())
                .Should().BeEquivalentTo(SolutionLayout.Default.ApplicationFolders);
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_GivenMissingPreferencesUsesFolderDefaultsAndInvalidOverridesAreRejected()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            fixture.CopyTo(root);
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
            SolutionWorkspace.Load(root).Layout.Should().BeEquivalentTo(SolutionLayout.Default);
            Application(prompter, root).Run(["scaffold", "api", "Orders"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            File.Exists(Path.Combine(root, "src", "Api", "Orders", "Orders.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();

            File.WriteAllText(Path.Combine(root, ".anarchy-toolkit.json"),
                """{ "layering": { "mode": "prefix", "libraryRoot": "src/Library" } }""");
            Application(prompter, root).Run(["scaffold", "api", "Billing"], interactive: false).Should().Be(1);
            prompter.Errors.Should().ContainMatch("*prefix mode cannot have libraryRoot*");
            Directory.Exists(Path.Combine(root, "src", "Web.Api.Billing")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_GivenFolderLayoutWithoutAnApplicationFolder_ExplainsTheMissingMapping()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            fixture.CopyTo(root);
            File.WriteAllText(Path.Combine(root, ".anarchy-toolkit.json"),
                """{ "layering": { "mode": "folder", "layers": ["Core", "Hosting", "Web"], "libraryRoot": "src/Library", "applicationFolders": { "src/Jobs": "Hosting" } } }""");

            Application(prompter, root).Run(["scaffold", "api", "Orders"], interactive: false).Should().Be(1);
            prompter.Errors.Should().ContainMatch("*maps no Api folder to the Web layer*\"src/Api\": \"Web\"*");
            Application(prompter, root).Run(["scaffold", "job", "Reports"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            File.Exists(Path.Combine(root, "src", "Jobs", "Reports", "Reports.csproj")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_InstallsSkillsIntoTheSolutionWithoutOverwriting()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            fixture.CopyTo(root);
            Directory.Exists(Path.Combine(root, ".agents")).Should().BeFalse();

            Application(prompter, Path.Combine(root, "build")).Run(["skills", "install"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            prompter.NextSteps.Should().Contain(step => step.StartsWith("make anarchy TARGET='ValidateAgentSkills'"));
            prompter.NextSteps.Should().Contain(step => step.StartsWith("make anarchy TARGET='SyncAgentSkills'"));
            File.Exists(Path.Combine(root, ".agents", "skills", "anarchy-develop", "SKILL.md")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".agents", "skills", "anarchy-conventions", "references", "conventions", "layering.md")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".agents", "skills", "anarchy-conventions", "references", "decisions", "README.md")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".agents", "skills", "anarchy-adopt", "references", "guides", "onboarding-recipes.md")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".agents", "skills", "anarchy", "references", "guides", "mental-model.md")).Should().BeTrue();

            Application(prompter, root).Run(["skills", "install"], interactive: false).Should().Be(1);
            prompter.Errors.Should().ContainMatch("*overwrite*");
            Application(prompter).Run(["skills", "install", "--output", Path.Combine(root, "missing")], interactive: false).Should().Be(1);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_InstallsSkillsWithCustomDirectoriesAndSuggestsOrderedCanonicalTargets()
    {
        var root = TempRoot();
        try
        {
            fixture.CopyTo(root);
            File.WriteAllText(Path.Combine(root, ".anarchy-toolkit.json"),
                """{ "agentSkills": { "directories": [".agents/team skills", ".agents/reviewer skills"] } }""");
            var install = new ScriptedPrompter();
            Application(install, root).Run(["skills", "install"], interactive: false)
                .Should().Be(0, string.Join("\n", install.Errors));
            File.Exists(Path.Combine(root, ".agents", "team skills", "anarchy", "SKILL.md")).Should().BeTrue();

            var log = Path.Combine(root, "suggested-skills.log");
            if (!OperatingSystem.IsWindows())
                File.WriteAllText(Path.Combine(root, "scripts", "build.anarchy.sh"), "#!/usr/bin/env bash\nprintf '<%s>\\n' \"$@\" > \"$MAKE_TEST_LOG\"\n");
            foreach (var target in new[] { "ValidateAgentSkills", "SyncAgentSkills" })
            {
                var suggestion = install.NextSteps.Should().ContainSingle(step => step.StartsWith($"make anarchy TARGET='{target} "))
                    .Which;
                suggestion.Should().Contain("--AgentSkillsDirectories");
                if (!OperatingSystem.IsWindows())
                {
                    var result = LocalProcessTasks.Run(root, "/bin/bash", ["-c", suggestion], new Dictionary<string, string> {
                        ["MAKE_TEST_LOG"] = log,
                        ["ANARCHY_RELOAD_ENV"] = "false"
                    });
                    result.ExitCode.Should().Be(0, result.Output + result.Error);
                    File.ReadAllText(log).Should().Be($"<{target}>\n<--AgentSkillsDirectories>\n<.agents/team skills>\n<.agents/reviewer skills>\n");
                }
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData(new[] { "scaffold", "library", "Store", "--layer", "Infrastructure" }, ScaffoldTarget.Library, false)]
    [InlineData(new[] { "scaffold", "tests", "unit" }, ScaffoldTarget.Tests, false)]
    [InlineData(new[] { "scaffold", "tests" }, ScaffoldTarget.Tests, true)]
    [InlineData(new[] { "scaffold", "library", "Store", "--layer", "Core", "--dry-run", "--save-preferences" }, ScaffoldTarget.Library, false)]
    public void CommandLine_ParsesLibraryAndTestScaffolds(string[] args, ScaffoldTarget target, bool interactive)
    {
        var command = ToolCommandLine.Parse(args);

        command.Mode.Should().Be(ToolMode.Scaffold);
        command.ScaffoldTarget.Should().Be(target);
        command.IsInteractive.Should().Be(interactive);
        if (args.Contains("--dry-run")) command.DryRun.Should().BeTrue();
        if (args.Contains("--save-preferences")) command.SavePreferences.Should().BeTrue();
    }

    [Fact]
    public void CommandLine_RejectsMalformedLibraryAndTestScaffolds()
    {
        ToolCommandLine.Parse(["scaffold", "library", "Store"]).Mode.Should().Be(ToolMode.Invalid);
        ToolCommandLine.Parse(["scaffold", "library", "Store", "--layer", "Core", "--layer", "Web"]).Mode.Should().Be(ToolMode.Invalid);
        ToolCommandLine.Parse(["scaffold", "tests", "acceptance"]).Mode.Should().Be(ToolMode.Invalid);
        ToolCommandLine.Parse(["scaffold", "api", "Orders", "--dry-run"]).Mode.Should().Be(ToolMode.Invalid);
    }

    [Fact]
    public void Run_ScaffoldsLibrariesWithinFolderPrefixAndCustomLayouts()
    {
        var roots = new[] { TempRoot(), TempRoot(), TempRoot() };
        try
        {
            var prompter = new ScriptedPrompter();
            fixture.CopyTo(roots[0]);
            Application(prompter, roots[0]).Run(["scaffold", "library", "Query", "--layer", "Infrastructure"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            var folderProject = Path.Combine(roots[0], "src", "Library", "Infrastructure", "Query", "Query.csproj");
            File.Exists(folderProject).Should().BeTrue();
            File.ReadAllText(folderProject).Should().Contain("ProjectReference Include=").And.Contain("Core.csproj");
            File.ReadAllText(Path.Combine(roots[0], "MyApp.sln")).Should().Contain("Infrastructure\\Query\\Query.csproj");
            Application(prompter, roots[0]).Run(["scaffold", "library", "Query", "--layer", "Infrastructure"], interactive: false)
                .Should().Be(1);

            ToolkitSolutionFixture.CreateLibraryWorkspace(roots[1], new SolutionLayout(LayeringMode.Prefix, ["Core", "Infrastructure"]));
            Application(prompter, roots[1]).Run([
                "scaffold", "library", "Data", "--layer", "Infrastructure", "--layering", "prefix", "--layering-layers", "[\"Core\",\"Infrastructure\"]"
            ], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            var prefixProject = Path.Combine(roots[1], "src", "Infrastructure.Data", "Infrastructure.Data.csproj");
            File.Exists(prefixProject).Should().BeTrue();
            File.ReadAllText(prefixProject).Should().Contain("ProjectReference Include=").And.Contain("Core.csproj");
            File.ReadAllText(Path.Combine(roots[1], "MyApp.sln")).Should().Contain("Infrastructure.Data\\Infrastructure.Data.csproj");

            ToolkitSolutionFixture.CreateLibraryWorkspace(roots[2], SolutionLayout.Default with { Root = "code", LibraryRoot = "code/Shared" });
            Application(prompter, roots[2]).Run([
                "scaffold", "library", "Events", "--layer", "Core", "--layering-root", "code", "--layering-library-root", "code/Shared",
                "--layering-application-folders", "{\"code/Api\":\"Web\",\"code/Jobs\":\"Hosting\",\"code/Ui\":\"Web\"}"
            ], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            File.Exists(Path.Combine(roots[2], "code", "Shared", "Core", "Events", "Events.csproj")).Should().BeTrue();
            File.ReadAllText(Path.Combine(roots[2], "MyApp.sln")).Should().Contain("code\\Shared\\Core\\Events\\Events.csproj");
        }
        finally
        {
            foreach (var root in roots)
                if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_LibraryDryRunWritesNothingAndSavingPersistsOnlyExplicitLayoutValues()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            fixture.CopyTo(root);
            var args = new[] {
                "scaffold", "library", "Events", "--layer", "Infrastructure", "--layering", "prefix",
                "--layering-layers", "[\"Core\",\"Infrastructure\"]", "--save-preferences", "--dry-run"
            };

            Application(prompter, root).Run(args, interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            File.Exists(Path.Combine(root, "src", "Infrastructure.Events", "Infrastructure.Events.csproj")).Should().BeFalse();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
            prompter.Progress.Should().Contain("Dry run: nothing is written.");

            Application(prompter, root).Run(args[..^1], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            var saved = AutomationPreferences.Read(root);
            saved.Saved["layering"]!.AsObject().Select(property => property.Key).Should().Equal("mode", "layers");
            saved.Saved["layering"]!["mode"]!.GetValue<string>().Should().Be("prefix");
            File.Exists(Path.Combine(root, "src", "Infrastructure.Events", "Infrastructure.Events.csproj")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_ScaffoldsTestProjectsUsingConfiguredAliasesAndTestsRoot()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            fixture.CopyTo(root);
            File.WriteAllText(Path.Combine(root, ".fallout", "parameters.json"),
                "{ \"Solution\": \"MyApp.sln\", \"TestKindAliases\": [\"EndToEnd=Acceptance\"] }");

            Application(prompter, root).Run(["scaffold", "tests", "endtoend"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));

            var project = Path.Combine(root, "tests", "Acceptance", "Acceptance.csproj");
            File.Exists(project).Should().BeTrue();
            File.ReadAllText(project).Should().Contain("<TargetFramework>net10.0</TargetFramework>")
                .And.NotContain("PackageReference");
            File.ReadAllText(Path.Combine(root, "MyApp.sln")).Should().Contain("tests\\Acceptance\\Acceptance.csproj");

            File.WriteAllText(Path.Combine(root, ".fallout", "parameters.json"), "{ \"Solution\": \"MyApp.sln\" }");
            Application(prompter, root).Run(["config", "set", "build.testsRoot", "\"quality/tests\""], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["config", "set", "tests.kindAliases", "[\"Integration=Contract\"]"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "tests", "integration"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            File.Exists(Path.Combine(root, "quality", "tests", "Contract", "Contract.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_TestScaffoldPromptsForKindOnlyWhenInteractiveAndDryRunDoesNotSave()
    {
        var root = TempRoot();
        try
        {
            fixture.CopyTo(root);

            var noninteractive = new ScriptedPrompter();
            Application(noninteractive, root).Run(["scaffold", "tests"], interactive: false).Should().Be(1);
            noninteractive.Errors.Should().ContainMatch("Interactive input is unavailable*");

            var preview = new ScriptedPrompter("Integration", true);
            Application(preview, root).Run(["scaffold", "tests", "--dry-run", "--save-preferences"], interactive: true)
                .Should().Be(0, string.Join("\n", preview.Errors));
            File.Exists(Path.Combine(root, "tests", "Integration", "Integration.csproj")).Should().BeFalse();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();

            Application(new ScriptedPrompter(), root).Run(["scaffold", "tests", "unit", "--save-preferences"], interactive: false)
                .Should().Be(0);
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();

            var interactive = new ScriptedPrompter("Integration", true);
            Application(interactive, root).Run(["scaffold", "tests"], interactive: true)
                .Should().Be(0, string.Join("\n", interactive.Errors));
            File.Exists(Path.Combine(root, "tests", "Integration", "Integration.csproj")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
