using Anarchy.Toolkit.Automation.Build.Cli.Command;
using Anarchy.Toolkit.Automation.Build.Cli.Service;
using Anarchy.Toolkit.Automation.Build.Cli.Wizard;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using Spectre.Console;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Cli;

public class ToolkitToolTest
{
    private static readonly ToolkitPackageVersions Versions = new(new Dictionary<string, string> {
        ["Anarchy.Toolkit.Automation.Build"] = "1.2.3",
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
    [InlineData(new[] { "bootstrap", "MyApp", "--with-skills", "--output", "dir" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--with-skills", "--with-skills" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layering", "folder" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--layering", "prefix" }, ToolMode.Bootstrap, false)]
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

    [Fact]
    public void Boilerplate_PrintsPlainOutputWithoutTheBanner()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var prompter = new ScriptedPrompter();
            string Run(int expectedExitCode, params string[] args)
            {
                var output = new StringWriter();
                new ToolApplication(prompter, Versions, root, output).Run(args, interactive: false).Should().Be(expectedExitCode);
                return output.ToString();
            }

            Run(0, "boilerplate", "list").Should().Contain("Makefile\n").And.Contain("scripts/build.anarchy.sh");
            Run(0, "boilerplate", "show", "Makefile").Should().Be(BoilerplateCatalog.Read("Makefile"));
            Run(1, "boilerplate", "diff", "Makefile").Should().Be("missing: Makefile\n");
            SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = "Makefile" });
            Run(0, "boilerplate", "diff", "Makefile").Should().Be("identical: Makefile\n");
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
    public void BootstrapWizard_ValidatesAnswersAndPlansApplicationsInKindOrder()
    {
        var root = TempRoot();
        var prompter = new ScriptedPrompter(
            "bad name", "MyApp", root,
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
            var prompter = new ScriptedPrompter("MyApp", existing, "", LayeringMode.Prefix, "Core Web", Array.Empty<object>(), false, false, true);
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
        var prompter = new ScriptedPrompter("MyApp", root, LayeringMode.Folder, "", Array.Empty<object>(), false, false, false);

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
                "MyApp", root, LayeringMode.Folder, "",
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

    [Fact]
    public void Run_ScaffoldsAnApiWithOrWithoutTheSwaggerTheme()
    {
        var root = TempRoot();
        try
        {
            Application(new ScriptedPrompter()).Run(["bootstrap", "MyApp", "--output", root], interactive: false).Should().Be(0);

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
            Application(prompter).Run(["bootstrap", "MyApp", "--output", root], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));

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
            Application(new ScriptedPrompter()).Run(["bootstrap", "MyApp", "--output", root], interactive: false)
                .Should().Be(0);

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
            Application(prompter).Run(["bootstrap", "MyApp", "--output", root], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
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
            Application(prompter).Run(["bootstrap", "MyApp", "--output", root], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
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
            Application(prompter).Run(["bootstrap", "MyApp", "--output", root], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));
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
            var bootstrap = new ScriptedPrompter();
            Application(bootstrap).Run(["bootstrap", "MyApp", "--output", root], interactive: false)
                .Should().Be(0, string.Join("\n", bootstrap.Errors));
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
}
