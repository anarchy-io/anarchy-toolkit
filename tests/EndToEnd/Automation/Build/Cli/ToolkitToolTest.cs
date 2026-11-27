using Anarchy.Toolkit.Automation.Build.Cli.Command;
using Anarchy.Toolkit.Automation.Build.Cli.Service;
using Anarchy.Toolkit.Automation.Build.Cli.Wizard;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Automation.Helpers.Solutions;
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
    [InlineData(new[] { "bootstrap", "MyApp", "--no-tests" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--no-tests", "--no-tests" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--with-mock-server", "--isolated-app-settings" }, ToolMode.Bootstrap, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--with-mock-server", "--with-mock-server" }, ToolMode.Invalid, false)]
    [InlineData(new[] { "bootstrap", "MyApp", "--isolated-app-settings", "--isolated-app-settings" }, ToolMode.Invalid, false)]
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

        SolutionLayout.Default.ProjectPath(api).Should().Be("src/Apps/Apis/Orders/Orders.csproj");
        SolutionLayout.Default.RootNamespace(api).Should().Be("Apps.Apis.Orders");
        SolutionLayout.Default.RootNamespace(job).Should().Be("Apps.Jobs.Reports");
        SolutionLayout.Default.RootNamespace(api, "MyApp").Should().Be("MyApp.Apps.Apis.Orders");
        SolutionLayout.Default.LibraryNamespace("Core", "MyApp").Should().Be("MyApp.Library.Core");
        SolutionLayout.Namespace("MyApp", "tests/Unit").Should().Be("MyApp.Tests.Unit");
        SolutionLayout.Default.SupportsSharedAppSettings.Should().BeTrue();
        (SolutionLayout.Default with { Layers = ["Core", "Infrastructure", "Runtime", "Apps"] }).SupportsSharedAppSettings.Should().BeFalse();
        SolutionLayout.Default.LibraryProjectPath("Core").Should().Be("src/Library/Core/Core.csproj");
        SolutionLayout.Default.LibraryRoot.Should().Be("src/Library");
        SolutionLayout.Default.ApplicationFolders.Should().Equal(new Dictionary<string, string> {
            ["src/Apps/Apis"] = "Apps",
            ["src/Apps/Jobs"] = "Apps",
            ["src/Apps/Ui"] = "Apps"
        });
        SolutionLayout.Default.LibraryLayers.Should().Equal("Core", "Infrastructure", "Bootstrap", "Runtime");
        SolutionLayout.Default.LibraryOf(ApplicationKind.Api).Should().Be("src/Library/Runtime/Runtime.csproj");
        SolutionLayout.Default.LibraryOf(ApplicationKind.Job).Should().Be("src/Library/Runtime/Runtime.csproj");
        new SolutionLayout(LayeringMode.Folder, ["Core", "Runtime"]).ApplicationFolders.Should().BeEmpty();
        new SolutionLayout(LayeringMode.Folder, ["Core", "Runtime"]).LibraryOf(ApplicationKind.Api).Should().BeNull();

        var prefix = new SolutionLayout(LayeringMode.Prefix, SolutionLayout.DefaultLayers);
        prefix.LibraryRoot.Should().BeNull();
        prefix.ApplicationFolders.Should().BeEmpty();
        prefix.ProjectPath(job).Should().Be("src/Apps.Job.Reports/Apps.Job.Reports.csproj");
        prefix.RootNamespace(job).Should().Be("Apps.Job.Reports");
        prefix.RootNamespace(job, "MyApp").Should().Be("MyApp.Apps.Job.Reports");
        prefix.LibraryNamespace("Core", "MyApp").Should().Be("MyApp.Library.Core");
        prefix.SupportsSharedAppSettings.Should().BeTrue();
        prefix.LibraryProjectPath("Core").Should().Be("src/Library.Core/Library.Core.csproj");
        prefix.LibraryOf(ApplicationKind.Ui).Should().Be("src/Library.Runtime/Library.Runtime.csproj");
        LayeringTasks.GetLayer("Library.Runtime", prefix.LibraryProjectPath("Runtime"), prefix).Should().Be("Runtime");
        LayeringTasks.GetLayer(prefix.ProjectName(job), prefix.ProjectPath(job), prefix).Should().Be("Apps");
        (prefix with { Layers = ["Core", "Runtime"] }).Supports(ApplicationKind.Job).Should().BeFalse();
    }

    [Fact]
    public void SingleAppPreset_UsesTheApplicationLayerAndAcceptsOneCustomFolder()
    {
        var api = SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Api, "Shop");
        api.Root.Should().Be("src");
        api.LibraryRoot.Should().Be("src");
        api.Layers.Should().Equal("Core", "Infrastructure", "Bootstrap", "Runtime", "Apps");
        api.LibraryLayers.Should().Equal("Core", "Infrastructure", "Bootstrap", "Runtime");
        api.ApplicationFolders.Should().Equal(new Dictionary<string, string> { ["src/Shop"] = "Apps" });
        api.LibraryOf(ApplicationKind.Api).Should().Be("src/Runtime/Runtime.csproj");
        api.RootNamespace(new ApplicationSpec(ApplicationKind.Api, "Store")).Should().Be("Shop.Store");
        api.RootNamespace(new ApplicationSpec(ApplicationKind.Api, "Store"), "MyApp").Should().Be("MyApp.Shop.Store");
        api.LibraryNamespace("Core", "MyApp").Should().Be("MyApp.Core");
        api.SupportsSharedAppSettings.Should().BeFalse();
        api.ProjectPath(new ApplicationSpec(ApplicationKind.Api, "Store")).Should().Be("src/Shop/Store/Store.csproj");
        LayeringConfiguration.Validate(api).Should().BeEmpty();

        var job = SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Job);
        job.Layers.Should().Equal("Core", "Infrastructure", "Bootstrap", "Runtime", "Apps");
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
            "Orders", true, false, false, false, true, "orders", "Billing", false, false, false, false,
            "Billing", "Admin", false, false,
            false, true, false, false, false, true);

        var plan = new BootstrapWizard(prompter).Run();

        plan.Should().NotBeNull();
        plan!.Name.Should().Be("MyApp");
        plan.RootDirectory.Should().Be(Path.GetFullPath(root));
        plan.Layout.Should().BeEquivalentTo(SolutionLayout.Default);
        plan.Applications.Select(plan.Layout.ProjectPath).Should().Equal(
            "src/Apps/Apis/Orders/Orders.csproj", "src/Apps/Apis/Billing/Billing.csproj", "src/Apps/Ui/Admin/Admin.csproj");
        plan.TestKinds.Should().Equal(SolutionPlan.AllTestKinds);
        prompter.Confirmations.Should().Contain(entry => entry.Prompt == "Create the Unit, Integration, EndToEnd test projects?" && entry.DefaultValue);
        prompter.ValidationErrors.Should().HaveCount(4)
            .And.ContainMatch("Solution names*").And.ContainMatch("*reserved*").And.ContainMatch("*orders already exists*")
            .And.ContainMatch("*Billing already exists*");
        prompter.ShownPlan.Should().Be(plan);
        plan.Ports.Values.Should().Equal((5000, 5001), (5002, 5003), (5004, 5005));
        plan.Applications.Select(app => (app.Name, app.Swagger, app.SwaggerTheme)).Should().Equal(
            ("Orders", true, false), ("Billing", false, true), ("Admin", true, true));
        prompter.Confirmations.Where(entry => entry.Prompt == "Include Swagger?").Should().HaveCount(2)
            .And.OnlyContain(entry => entry.DefaultValue);
        prompter.Confirmations.Where(entry => entry.Prompt == "Use the toolkit's Swagger UI theme?").Should().ContainSingle()
            .Which.DefaultValue.Should().BeTrue();
    }

    [Fact]
    public void BootstrapWizard_DefaultsDirectoryToSolutionNameAndRejectsNonEmptyDirectories()
    {
        var existing = TempRoot();
        Directory.CreateDirectory(existing);
        File.WriteAllText(Path.Combine(existing, "keep"), "");
        try
        {
            var prompter = new ScriptedPrompter("MyApp", existing, "", false, LayeringMode.Prefix, "Core Runtime", Array.Empty<object>(), false, false, false, false, true);
            var plan = new BootstrapWizard(prompter).Run();
            plan!.RootDirectory.Should().Be(Path.GetFullPath("MyApp"));
            plan.Layout.Should().BeEquivalentTo(new SolutionLayout(LayeringMode.Prefix, ["Core", "Runtime"]));
            plan.Applications.Should().BeEmpty();
            plan.TestKinds.Should().BeEmpty();
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
            "Shop", root, true, ApplicationKind.Api, "Shop", LayeringMode.Folder, "", "Orders", true, true, false, false, true, false, false, false, true);

        var plan = new BootstrapWizard(prompter).Run();

        plan.Should().NotBeNull();
        plan!.Layout.Should().BeEquivalentTo(SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Api, "Shop"));
        plan.Applications.Select(plan.Layout.ProjectPath).Should().Equal("src/Shop/Orders/Orders.csproj");
        plan.Layout.Layers.Should().Equal("Core", "Infrastructure", "Bootstrap", "Runtime", "Apps");
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
                "Shop", root, true, ApplicationKind.Api, "Shop", LayeringMode.Folder, "", "Orders", true, true, false, false, false, false, false, savePreferences, true);
            Application(bootstrap).Run(["bootstrap"], interactive: true).Should().Be(0, string.Join("\n", bootstrap.Errors));
            File.Exists(Path.Combine(root, "src", "Shop", "Orders", "Orders.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().Be(savePreferences);
            if (savePreferences)
                AutomationPreferences.Read(root).ResolveLayout().Should().BeEquivalentTo(
                    SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, ApplicationKind.Api, "Shop"));

            var scaffold = new ScriptedPrompter(ApplicationKind.Ui, "Admin", false, true);
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
        var prompter = new ScriptedPrompter("MyApp", root, false, LayeringMode.Folder, "", Array.Empty<object>(), false, true, false, false, false, false);

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
                "Orders", true, true, false, false, false, "Billing", JobExecutionMode.Worker, false, false,
                false, true, false, false, false, true);

            Application(prompter).Run(["bootstrap"], interactive: true).Should().Be(0, string.Join("\n", prompter.Errors));

            prompter.BannersShown.Should().Be(1);
            prompter.Progress.Should().Contain("Creating src/Apps/Apis/Orders/Orders.csproj");
            var solution = File.ReadAllText(Path.Combine(root, "MyApp.sln"));
            solution.Should().Contain("_build.csproj").And.Contain("Bootstrap.csproj").And.Contain("Orders.csproj").And.Contain("Billing.csproj");
            File.Exists(Path.Combine(root, "src", "Core", "Core.csproj")).Should().BeFalse();
            Directory.Exists(Path.Combine(root, ".agents")).Should().BeFalse();
            File.ReadAllText(Path.Combine(root, "src", "Library", "Runtime", "Runtime.csproj")).Should().Contain(@"..\Bootstrap\Bootstrap.csproj");
            Directory.Exists(Path.Combine(root, "src", "Library", "Apps")).Should().BeFalse();
            Directory.Exists(Path.Combine(root, "src", "Library", "Automation")).Should().BeFalse();
            Directory.Exists(Path.Combine(root, "samples")).Should().BeFalse();
            solution.Should().Contain($"= \"{SolutionItems.FolderName}\", \"{SolutionItems.FolderName}\"")
                .And.Contain("Makefile = Makefile").And.Contain(@"scripts\build.anarchy.sh = scripts\build.anarchy.sh")
                .And.Contain("global.json = global.json").And.Contain("nuget.config = nuget.config")
                .And.Contain("Directory.Build.props = Directory.Build.props")
                .And.Contain("Directory.Packages.props = Directory.Packages.props")
                .And.Contain(".editorconfig = .editorconfig").And.NotContain(".anarchy-toolkit.json = ");
            foreach (var kind in SolutionPlan.AllTestKinds)
            {
                File.Exists(Path.Combine(root, "tests", kind, kind + ".csproj")).Should().BeTrue();
                solution.Should().Contain($"tests\\{kind}\\{kind}.csproj");
            }
            var packages = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
            packages.Should().Contain("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Automation.Build\" Version=\"1.2.3\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Runtime.Web\" Version=\"2.0.0\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Runtime\" Version=\"3.0.0\" />")
                .And.NotContain("Anarchy.Toolkit.Runtime.Web.Ui");
            File.ReadAllText(Path.Combine(root, "build", "_build.csproj"))
                .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Automation.Build\" />").And.NotContain("Version=");
            File.ReadAllText(Path.Combine(root, "build", "Build.cs"))
                .Should().Contain("class Build : AnarchyBuild,").And.Contain("using Anarchy.Toolkit.Automation.Build.Component;")
                .And.NotContain("using Fallout.Common;").And.NotContain(": FalloutBuild");
            var api = File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Orders", "Orders.csproj"));
            api.Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Runtime.Web\" />").And.NotContain("Version=").And.Contain("<IsPackable>false</IsPackable>")
                .And.Contain("<RootNamespace>MyApp.Apps.Apis.Orders</RootNamespace>").And.Contain(@"..\..\..\Library\Runtime\Runtime.csproj");
            // Isolated settings: the application keeps its own, and nothing is shared or recorded.
            File.Exists(Path.Combine(root, "src", "Apps", "Apis", "Orders", "Composition", "DependencyInjection", "ModuleAppSettings.cs")).Should().BeTrue();
            File.Exists(Path.Combine(root, "src", "Apps", "Apis", "Orders", "appsettings.json")).Should().BeTrue();
            File.Exists(Path.Combine(root, "src", "Library", "Infrastructure", "Entity", "Configuration", "AppSettings.cs")).Should().BeFalse();
            File.ReadAllText(Path.Combine(root, "Directory.Build.props")).Should().NotContain("appsettings*.json");
            File.ReadAllText(Path.Combine(root, "src", "Library", "Core", "Core.csproj"))
                .Should().Contain("<RootNamespace>MyApp.Library.Core</RootNamespace>").And.Contain("<AssemblyName>MyApp.Library.Core</AssemblyName>");
            // The build project sits in the solution root, and shows Fallout's parameters.
            solution.Should().Contain(@"""_build"", ""build\_build.csproj""").And.NotContain(@"= ""build"", ""build""");
            File.ReadAllText(Path.Combine(root, "build", "_build.csproj"))
                .Should().Contain(@"<None Include=""..\.fallout\parameters.json"" Link="".fallout\parameters.json"" />");
            // Every test kind references the common test project, which holds the toolkit's test kits.
            var common = File.ReadAllText(Path.Combine(root, "tests", "Common", "Common.csproj"));
            common.Should().Contain("<IsPackable>false</IsPackable>").And.Contain("<IsTestProject>false</IsTestProject>")
                .And.Contain("<RootNamespace>MyApp.Tests.Common</RootNamespace>")
                .And.Contain("<PackageReference Include=\"Anarchy.Toolkit.Testing\" />")
                .And.Contain("<PackageReference Include=\"Anarchy.Toolkit.Testing.Runtime.Web\" />")
                .And.NotContain("Anarchy.Toolkit.Testing.MockServer");
            File.Exists(Path.Combine(root, "tests", "Common", "README.md")).Should().BeTrue();
            solution.Should().Contain(@"tests\Common\Common.csproj").And.NotContain("MockServer.csproj");
            File.ReadAllText(Path.Combine(root, "tests", "Unit", "Unit.csproj"))
                .Should().Contain("<RootNamespace>MyApp.Tests.Unit</RootNamespace>").And.Contain(@"..\Common\Common.csproj");
            packages.Should().Contain("<PackageVersion Include=\"Anarchy.Toolkit.Testing\" Version=\"6.0.0\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Testing.Runtime.Web\" Version=\"6.1.0\" />");
            Directory.Exists(Path.Combine(root, "tests", "MockServer")).Should().BeFalse();
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Jobs", "Billing", "Billing.csproj"))
                .Should().Contain(@"..\..\..\Library\Runtime\Runtime.csproj").And.Contain("<OutputType>Exe</OutputType>");
            var parameters = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".fallout", "parameters.json")))!.AsObject();
            parameters["Solution"]!.GetValue<string>().Should().Be("MyApp.sln");
            parameters.Select(parameter => parameter.Key).Should().NotContain(key => key.StartsWith("Layering"));
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
            prompter.Confirmations.Should().Contain(entry => entry.Prompt.Contains("Save the selected layering preferences") && !entry.DefaultValue);

            // Scaffold from a subdirectory: the solution is found upwards and its layout and ports are respected.
            var scaffold = new ScriptedPrompter(ApplicationKind.Ui, "Orders", "Admin", false, true);
            Application(scaffold, Path.Combine(root, "src", "Apps", "Apis")).Run(["scaffold"], interactive: true)
                .Should().Be(0, string.Join("\n", scaffold.Errors));

            scaffold.ValidationErrors.Should().ContainSingle().Which.Should().Contain("Orders already exists");
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Ui", "Admin", "EntryPointLocal.cs")).Should().Contain("args, 5002, 5003");
            File.ReadAllText(Path.Combine(root, "MyApp.sln")).Should().Contain("Admin.csproj");
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Ui", "Admin", "Admin.csproj")).Should().NotContain("Version=");
            var updated = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
            File.ReadAllLines(Path.Combine(root, "Directory.Packages.props"))
                .Should().Contain("    <PackageVersion Include=\"Anarchy.Toolkit.Runtime.Web.Ui\" Version=\"2.1.0\" />");
            updated.Split("Include=\"Anarchy.Toolkit.Runtime.Web\"").Should().HaveCount(2);

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
            var prompter = new ScriptedPrompter("MyApp", root, false, LayeringMode.Prefix, "Core, Runtime, Apps",
                new object[] { ApplicationKind.Api }, "Orders", false, false, false, false, false, false, true, true, true);

            Application(prompter).Run(["bootstrap"], interactive: true).Should().Be(0, string.Join("\n", prompter.Errors));

            File.ReadAllText(Path.Combine(root, "src", "Apps.Api.Orders", "Apps.Api.Orders.csproj"))
                .Should().Contain(@"..\Library.Runtime\Library.Runtime.csproj").And.Contain("<RootNamespace>MyApp.Apps.Api.Orders</RootNamespace>");
            File.ReadAllText(Path.Combine(root, "src", "Library.Core", "Library.Core.csproj")).Should().Contain("<RootNamespace>MyApp.Library.Core</RootNamespace>");
            File.Exists(Path.Combine(root, "src", "Library.Core", "Library.Core.csproj")).Should().BeTrue();
            Directory.Exists(Path.Combine(root, "src", "Library.Apps")).Should().BeFalse();
            File.Exists(Path.Combine(root, "tests", "Unit", "Unit.csproj")).Should().BeFalse();
            var preferences = AutomationPreferences.Read(root);
            preferences.Saved["layering"]!.AsObject().Select(property => property.Key)
                .Should().Equal("mode", "layers");
            preferences.Saved["layering"]!["mode"]!.GetValue<string>().Should().Be("prefix");
            preferences.Saved["layering"]!["layers"]!.AsArray().Select(layer => layer!.GetValue<string>())
                .Should().Equal("Core", "Runtime", "Apps");
            var workspace = SolutionWorkspace.Load(root);
            workspace.Layout.Should().BeEquivalentTo(new SolutionLayout(LayeringMode.Prefix, ["Core", "Runtime", "Apps"]));
            workspace.UsedPorts.Should().BeEquivalentTo(new[] { 5000, 5001 });
            File.Exists(Path.Combine(root, ".agents", "skills", "anarchy", "SKILL.md")).Should().BeTrue();
            // Swagger was declined, so the theme question is skipped and nothing references Swagger.
            prompter.Confirmations.Should().Contain(entry => entry.Prompt == "Include Swagger?")
                .And.NotContain(entry => entry.Prompt == "Use the toolkit's Swagger UI theme?");
            var apiDirectory = Path.Combine(root, "src", "Apps.Api.Orders");
            File.ReadAllText(Path.Combine(apiDirectory, "Apps.Api.Orders.csproj")).Should().NotContain(ApplicationSpec.SwaggerPackageId);
            File.ReadAllText(Path.Combine(root, "Directory.Packages.props")).Should().NotContain(ApplicationSpec.SwaggerPackageId);
            Directory.EnumerateFiles(apiDirectory, "*.cs", SearchOption.AllDirectories)
                .Should().NotBeEmpty().And.OnlyContain(file => !File.ReadAllText(file).Contains("AddToolkitSwagger"));
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

            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Orders", "Orders.csproj"))
                .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Runtime.Web\" Version=\"2.0.0\" />");
            File.Exists(Path.Combine(root, "Directory.Packages.props")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Run_ScaffoldsAnApiWithOrWithoutSwagger()
    {
        var root = TempRoot();
        try
        {
            fixture.CopyTo(root);

            var prompter = new ScriptedPrompter();
            Application(prompter, root).Run(["scaffold", "api", "Orders"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "api", "Billing", "--no-swagger"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));

            var orders = Path.Combine(root, "src", "Apps", "Apis", "Orders");
            File.ReadAllText(Path.Combine(orders, "Orders.csproj"))
                .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Runtime.Web.Swagger\" />");
            File.ReadAllText(Path.Combine(orders, "Composition", "OrdersStartup.cs"))
                .Should().Contain("using Anarchy.Toolkit.Runtime.Web.Swagger.Extensions.Microsoft.Extensions.DependencyInjection;")
                .And.Contain("            .AddToolkitSwagger(ApiName, AppSettings, Environment);");
            var billing = Path.Combine(root, "src", "Apps", "Apis", "Billing");
            File.ReadAllText(Path.Combine(billing, "Billing.csproj")).Should().NotContain("Swagger");
            File.ReadAllText(Path.Combine(billing, "Composition", "BillingStartup.cs")).Should().NotContain("Swagger");
            File.ReadAllText(Path.Combine(root, "Directory.Packages.props"))
                .Should().Contain("<PackageVersion Include=\"Anarchy.Toolkit.Runtime.Web.Swagger\" Version=\"2.3.0\" />");

            Application(prompter, root).Run(["config", "set", "scaffolding.swagger", "false"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "api", "Claims"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "api", "Reports", "--swagger", "--save-preferences"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Claims", "Claims.csproj")).Should().NotContain("Swagger");
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Reports", "Reports.csproj")).Should().Contain("Swagger");
            AutomationPreferences.Read(root).Get<bool?>("scaffolding.swagger").Should().BeTrue();
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

            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Orders", "Composition", "OrdersStartup.cs"))
                .Should().NotContain("SwaggerThemeOptions");
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Billing", "Composition", "BillingStartup.cs"))
                .Should().Contain("using Anarchy.Toolkit.Runtime.Web.Swagger.Entity;")
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

            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Orders", "Composition", "OrdersStartup.cs"))
                .Should().Contain("theme.Enabled = false");
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Billing", "Composition", "BillingStartup.cs"))
                .Should().NotContain("theme.Enabled = false");

            Application(prompter, root).Run(["config", "set", "scaffolding.swaggerTheme", "true"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            Application(prompter, root).Run(["scaffold", "api", "Claims", "--no-swagger-theme"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Claims", "Composition", "ClaimsStartup.cs"))
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

            var declined = new ScriptedPrompter(ApplicationKind.Api, "Orders", true, false, false, false, true, false);
            Application(declined, root).Run(["scaffold"], interactive: true).Should().Be(0, string.Join("\n", declined.Errors));
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
            declined.Confirmations.Last().DefaultValue.Should().BeFalse();

            var accepted = new ScriptedPrompter(ApplicationKind.Api, "Billing", true, true, false, false, true, true);
            Application(accepted, root).Run(["scaffold"], interactive: true).Should().Be(0, string.Join("\n", accepted.Errors));
            accepted.Confirmations.Last().DefaultValue.Should().BeFalse();
            AutomationPreferences.Read(root).Get<bool?>("scaffolding.swaggerTheme").Should().BeTrue();
            AutomationPreferences.Read(root).Get<bool?>("scaffolding.swagger").Should().BeTrue();

            var withoutSwagger = new ScriptedPrompter(ApplicationKind.Api, "Claims", false, false, false, true, true);
            Application(withoutSwagger, root).Run(["scaffold"], interactive: true).Should().Be(0, string.Join("\n", withoutSwagger.Errors));
            withoutSwagger.Confirmations.Select(confirmation => confirmation.Prompt).Should().NotContain("Use the toolkit's Swagger UI theme?");
            AutomationPreferences.Read(root).Get<bool?>("scaffolding.swagger").Should().BeFalse();
            AutomationPreferences.Read(root).Get<bool?>("scaffolding.swaggerTheme").Should().BeTrue();
            File.ReadAllText(Path.Combine(root, "src", "Apps", "Apis", "Claims", "Claims.csproj")).Should().NotContain("Swagger");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Workspace_DiscoversLegacyAndExplicitEntryPointPorts()
    {
        var root = TempRoot();
        try
        {
            fixture.CopyTo(root);
            var legacy = Path.Combine(root, "src", "Apps", "Apis", "Legacy");
            var current = Path.Combine(root, "src", "Apps", "Apis", "Current");
            Directory.CreateDirectory(legacy);
            Directory.CreateDirectory(current);
            File.WriteAllText(Path.Combine(legacy, "Program.cs"), "WebAppEntryPoint.RunWebHost<Startup>(args);");
            File.WriteAllText(Path.Combine(current, "EntryPointLocal.cs"), "WebAppEntryPoint.RunWebHost<CurrentStartup>(args, 5002, 5003);");

            SolutionWorkspace.Load(root).UsedPorts.Should().BeEquivalentTo(new[] { 5000, 5001, 5002, 5003 });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CommandLine_ParsesEntryPointChoicesAndRejectsUnsupportedCombinations()
    {
        var api = ToolCommandLine.Parse(["scaffold", "api", "Orders", "--container", "--lambda-api"]);
        api.Application!.Container.Should().BeTrue();
        api.Application.ApiLambda.Should().BeTrue();
        var job = ToolCommandLine.Parse(["scaffold", "job", "Billing", "--job-mode", "once", "--lambda-event"]);
        job.Application!.JobMode.Should().Be(JobExecutionMode.Once);
        job.Application.EventLambda.Should().BeTrue();
        ToolCommandLine.Parse(["scaffold", "job", "Billing"]).Application!.JobMode.Should().Be(JobExecutionMode.Worker);
        ToolCommandLine.Parse(["scaffold", "ui", "Admin", "--lambda-api"]).Mode.Should().Be(ToolMode.Invalid);
        ToolCommandLine.Parse(["scaffold", "job", "Billing", "--job-mode", "other"]).Mode.Should().Be(ToolMode.Invalid);
        ToolCommandLine.Parse(["scaffold", "job", "Billing", "--job-mode", "0"]).Mode.Should().Be(ToolMode.Invalid);
        ToolCommandLine.Parse(["scaffold", "api", "Orders", "--container", "--container"]).Mode.Should().Be(ToolMode.Invalid);
    }

    [Fact]
    public void ScaffoldWizard_RecordsSelectedEntryPoints()
    {
        var workspace = new SolutionWorkspace("/work", "/work/Shop.sln", SolutionLayout.Default, [], []);
        var prompter = new ScriptedPrompter(ApplicationKind.Api, "Orders", true, true, true, true, true, false);

        var app = new ScaffoldWizard(prompter).Run(workspace);

        app!.Container.Should().BeTrue();
        app.ApiLambda.Should().BeTrue();
        prompter.ShownScaffold.Should().Be(app);
    }

    [Fact]
    public void CommandLine_ReadsBootstrapSkillsAndOutput()
    {
        var command = ToolCommandLine.Parse(["bootstrap", "MyApp", "-o", "dir", "--with-skills"]);
        command.Should().BeEquivalentTo(new ToolCommand(ToolMode.Bootstrap, "MyApp", "dir") { AgentSkills = true });
        ToolCommandLine.Parse(["bootstrap", "MyApp"]).AgentSkills.Should().BeFalse();
        ToolCommandLine.Parse(["bootstrap", "MyApp"]).TestProjects.Should().BeTrue();
        ToolCommandLine.Parse(["bootstrap", "MyApp", "--no-tests"]).TestProjects.Should().BeFalse();
        ToolCommandLine.Parse(["bootstrap", "MyApp"]).MockServer.Should().BeFalse();
        ToolCommandLine.Parse(["bootstrap", "MyApp", "--with-mock-server"]).MockServer.Should().BeTrue();
        ToolCommandLine.Parse(["bootstrap", "MyApp"]).IsolatedAppSettings.Should().BeFalse();
        ToolCommandLine.Parse(["bootstrap", "MyApp", "--isolated-app-settings"]).IsolatedAppSettings.Should().BeTrue();
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
        ToolCommandLine.Parse(["scaffold", "api", "Orders"]).SwaggerOverride.Should().BeNull();
        ToolCommandLine.Parse(["scaffold", "api", "Orders", "--no-swagger"]).SwaggerOverride.Should().BeFalse();
        ToolCommandLine.Parse(["scaffold", "api", "Orders", "--swagger"]).SwaggerOverride.Should().BeTrue();
        ToolCommandLine.Parse(["scaffold", "api", "Orders", "--no-swagger", "--swagger-theme"]).Mode.Should().Be(ToolMode.Invalid);
        ToolCommandLine.Parse(["scaffold", "job", "Orders", "--no-swagger"]).Mode.Should().Be(ToolMode.Invalid);
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

            File.Exists(Path.Combine(root, preset == "single-app" ? "src/Shop/Orders/Orders.csproj" : "src/Apps/Apis/Orders/Orders.csproj"))
                .Should().BeTrue();
            // A monorepo shares its settings by default, which is recorded as a preference even when layering is not saved.
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().Be(savePreferences || preset == "monorepo");
            AutomationPreferences.Read(root).Get<string>("scaffolding.appSettings").Should().Be(preset == "monorepo" ? "shared" : null);
            (AutomationPreferences.Read(root).Saved["layering"] is not null).Should().Be(savePreferences);
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

            var expected = new SolutionLayout(LayeringMode.Folder, ["Core", "Infrastructure", "Bootstrap", "Runtime", "Apps"]) {
                Root = "code",
                LibraryRoot = "code",
                ApplicationFolders = new Dictionary<string, string> { ["code/Cli"] = "Apps" }
            };
            SolutionWorkspace.Load(root).Layout.Should().BeEquivalentTo(expected);
            var build = Path.Combine(root, "build", "Build.cs");
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
                "bootstrap", "MyApp", "--output", root, "--layering", "prefix", "--layering-layers", "[\"Core\",\"Runtime\",\"Apps\"]"
            ], interactive: false).Should().Be(0, string.Join("\n", bootstrap.Errors));

            var scaffold = new ScriptedPrompter();
            Application(scaffold, root).Run([
                "scaffold", "api", "Orders", "--layering", "prefix", "--layering-layers", "[\"Core\",\"Runtime\",\"Apps\"]"
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
                        .And.Contain("<--LayeringLayers>\n<Core>\n<Runtime>\n<Apps>\n").And.Contain("<--LayeringRoot>\n<src>\n");
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
            File.Exists(Path.Combine(root, "src", "Apps", "Apis", "Orders", "Orders.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();

            File.WriteAllText(Path.Combine(root, ".anarchy-toolkit.json"),
                """{ "layering": { "mode": "prefix", "libraryRoot": "src/Library" } }""");
            Application(prompter, root).Run(["scaffold", "api", "Billing"], interactive: false).Should().Be(1);
            prompter.Errors.Should().ContainMatch("*prefix mode cannot have libraryRoot*");
            Directory.Exists(Path.Combine(root, "src", "Apps.Api.Billing")).Should().BeFalse();
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
                """{ "layering": { "mode": "folder", "layers": ["Core", "Runtime", "Apps"], "libraryRoot": "src/Library", "applicationFolders": { "src/Apps/Jobs": "Apps", "src/Apps/Ui": "Apps" } } }""");

            Application(prompter, root).Run(["scaffold", "api", "Orders"], interactive: false).Should().Be(1);
            prompter.Errors.Should().ContainMatch("*maps no Apis folder to the Apps layer*\"src/Apps/Apis\": \"Apps\"*");
            Application(prompter, root).Run(["scaffold", "job", "Reports"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            File.Exists(Path.Combine(root, "src", "Apps", "Jobs", "Reports", "Reports.csproj")).Should().BeTrue();
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
            File.ReadAllText(folderProject).Should().Contain("ProjectReference Include=").And.Contain("Core.csproj")
                .And.Contain("<RootNamespace>MyApp.Library.Infrastructure.Query</RootNamespace>");
            File.ReadAllText(Path.Combine(roots[0], "MyApp.sln")).Should().Contain("Infrastructure\\Query\\Query.csproj");
            Application(prompter, roots[0]).Run(["scaffold", "library", "Query", "--layer", "Infrastructure"], interactive: false)
                .Should().Be(1);

            ToolkitSolutionFixture.CreateLibraryWorkspace(roots[1], new SolutionLayout(LayeringMode.Prefix, ["Core", "Infrastructure"]));
            Application(prompter, roots[1]).Run([
                "scaffold", "library", "Data", "--layer", "Infrastructure", "--layering", "prefix", "--layering-layers", "[\"Core\",\"Infrastructure\"]"
            ], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));
            var prefixProject = Path.Combine(roots[1], "src", "Library.Infrastructure.Data", "Library.Infrastructure.Data.csproj");
            File.Exists(prefixProject).Should().BeTrue();
            File.ReadAllText(prefixProject).Should().Contain("ProjectReference Include=").And.Contain("Core.csproj")
                .And.Contain("<RootNamespace>MyApp.Library.Infrastructure.Data</RootNamespace>");
            File.ReadAllText(Path.Combine(roots[1], "MyApp.sln")).Should().Contain("Library.Infrastructure.Data\\Library.Infrastructure.Data.csproj");

            ToolkitSolutionFixture.CreateLibraryWorkspace(roots[2], SolutionLayout.Default with { Root = "code", LibraryRoot = "code/Shared" });
            Application(prompter, roots[2]).Run([
                "scaffold", "library", "Events", "--layer", "Core", "--layering-root", "code", "--layering-library-root", "code/Shared",
                "--layering-application-folders", "{\"code/Apps/Apis\":\"Apps\",\"code/Apps/Jobs\":\"Apps\",\"code/Apps/Ui\":\"Apps\"}"
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

            File.Exists(Path.Combine(root, "src", "Library.Infrastructure.Events", "Library.Infrastructure.Events.csproj")).Should().BeFalse();
            File.Exists(Path.Combine(root, ".anarchy-toolkit.json")).Should().BeFalse();
            prompter.Progress.Should().Contain("Dry run: nothing is written.");

            Application(prompter, root).Run(args[..^1], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            var saved = AutomationPreferences.Read(root);
            saved.Saved["layering"]!.AsObject().Select(property => property.Key).Should().Equal("mode", "layers");
            saved.Saved["layering"]!["mode"]!.GetValue<string>().Should().Be("prefix");
            File.Exists(Path.Combine(root, "src", "Library.Infrastructure.Events", "Library.Infrastructure.Events.csproj")).Should().BeTrue();
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

    [Fact]
    public void Run_BootstrapsSharedSettingsAndAMockServerThenScaffoldsWithTheSharedSettings()
    {
        var root = TempRoot();
        try
        {
            var prompter = new ScriptedPrompter();
            Application(prompter).Run(["bootstrap", "MyApp", "--output", root, "--with-mock-server"], interactive: false)
                .Should().Be(0, string.Join("\n", prompter.Errors));

            var infrastructure = Path.Combine(root, "src", "Library", "Infrastructure");
            var bootstrap = Path.Combine(root, "src", "Library", "Bootstrap");
            File.ReadAllText(Path.Combine(infrastructure, "Entity", "Configuration", "AppSettings.cs"))
                .Should().Contain("namespace MyApp.Library.Infrastructure.Entity.Configuration;");
            File.ReadAllText(Path.Combine(bootstrap, "DependencyInjection", "ModuleAppSettings.cs"))
                .Should().Contain("using MyApp.Library.Infrastructure.Entity.Configuration;")
                .And.Contain("namespace MyApp.Library.Bootstrap.DependencyInjection;");
            File.Exists(Path.Combine(bootstrap, "appsettings.json")).Should().BeTrue();
            File.Exists(Path.Combine(bootstrap, "appsettings.Development.json")).Should().BeTrue();
            foreach (var library in new[] { infrastructure, bootstrap })
                File.ReadAllText(Path.Combine(library, Path.GetFileName(library) + ".csproj"))
                    .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Bootstrap\" />");
            File.ReadAllText(Path.Combine(root, "src", "Library", "Core", "Core.csproj")).Should().NotContain("Anarchy.Toolkit.Bootstrap");
            File.ReadAllText(Path.Combine(root, "Directory.Build.props"))
                .Should().Contain(@"$(MSBuildThisFileDirectory)src\Library\Bootstrap\appsettings*.json").And.Contain("CopyToOutputDirectory=\"PreserveNewest\"")
                .And.EndWith("</Project>\n");
            File.ReadAllText(Path.Combine(root, "Directory.Packages.props"))
                .Should().Contain("<PackageVersion Include=\"Anarchy.Toolkit.Bootstrap\" Version=\"4.0.0\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Runtime.Web.MockServer.Ui\" Version=\"5.0.0\" />")
                .And.Contain("<PackageVersion Include=\"Anarchy.Toolkit.Testing.MockServer\" Version=\"6.2.0\" />");
            var preferences = AutomationPreferences.Read(root);
            preferences.Get<string>("scaffolding.appSettings").Should().Be("shared");
            preferences.Saved["layering"].Should().BeNull();
            File.ReadAllText(Path.Combine(root, "MyApp.sln")).Should().Contain(".anarchy-toolkit.json = .anarchy-toolkit.json")
                .And.Contain(@"tests\MockServer\MockServer.csproj");

            var mockServer = Path.Combine(root, "tests", "MockServer");
            File.ReadAllText(Path.Combine(mockServer, "MockServer.csproj")).Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk.Web\">")
                .And.Contain("<OutputType>Exe</OutputType>").And.Contain("<RootNamespace>MyApp.Tests.MockServer</RootNamespace>")
                .And.Contain("<PackageReference Include=\"Anarchy.Toolkit.Runtime.Web.MockServer.Ui\" />");
            File.ReadAllText(Path.Combine(mockServer, "EntryPointLocal.cs")).Should().Contain("namespace MyApp.Tests.MockServer;");
            File.Exists(Path.Combine(mockServer, "mocks", "ping.json")).Should().BeTrue();
            File.ReadAllText(Path.Combine(root, "tests", "Common", "Common.csproj"))
                .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Testing\" />")
                .And.Contain("<PackageReference Include=\"Anarchy.Toolkit.Testing.MockServer\" />")
                .And.NotContain("Anarchy.Toolkit.Testing.Runtime.Web");

            foreach (var (kind, name) in new[] { ("api", "Orders"), ("job", "Reports"), ("ui", "Admin") })
                Application(prompter, root).Run(["scaffold", kind, name], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            var api = Path.Combine(root, "src", "Apps", "Apis", "Orders");
            File.Exists(Path.Combine(api, "Composition", "DependencyInjection", "ModuleAppSettings.cs")).Should().BeFalse();
            File.Exists(Path.Combine(api, "Infrastructure", "Entity", "Configuration", "AppSettings.cs")).Should().BeFalse();
            File.Exists(Path.Combine(api, "appsettings.json")).Should().BeFalse();
            File.ReadAllText(Path.Combine(api, "Composition", "OrdersStartup.cs"))
                .Should().Contain("using MyApp.Library.Bootstrap.DependencyInjection;")
                .And.Contain("using MyApp.Library.Infrastructure.Entity.Configuration;");
            File.Exists(Path.Combine(root, "src", "Apps", "Jobs", "Reports", "appsettings.json")).Should().BeFalse();
            File.Exists(Path.Combine(root, "src", "Apps", "Ui", "Admin", "appsettings.json")).Should().BeFalse();

            // Isolated settings opt out: no shared files, preference or Directory.Build.props item.
            var isolated = TempRoot();
            try
            {
                Application(prompter).Run(["bootstrap", "MyApp", "--output", isolated, "--isolated-app-settings"], interactive: false)
                    .Should().Be(0, string.Join("\n", prompter.Errors));
                File.Exists(Path.Combine(isolated, "src", "Library", "Bootstrap", "appsettings.json")).Should().BeFalse();
                File.Exists(Path.Combine(isolated, ".anarchy-toolkit.json")).Should().BeFalse();
                File.ReadAllText(Path.Combine(isolated, "Directory.Build.props")).Should().NotContain("appsettings*.json");
                Directory.Exists(Path.Combine(isolated, "tests", "MockServer")).Should().BeFalse();
                Application(prompter, isolated).Run(["scaffold", "api", "Orders"], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));
                File.Exists(Path.Combine(isolated, "src", "Apps", "Apis", "Orders", "appsettings.json")).Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(isolated)) Directory.Delete(isolated, true);
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void BootstrapWizard_OffersSharedSettingsAndAMockServerForAMonorepo()
    {
        var root = TempRoot();
        var prompter = new ScriptedPrompter(
            "MyApp", root, false, LayeringMode.Folder, "", new object[] { ApplicationKind.Api }, "Orders", true, true, false, false, false,
            true, true, true, false, false, true);

        var plan = new BootstrapWizard(prompter).Run();

        plan!.SharedAppSettings.Should().BeTrue();
        plan.MockServer.Should().BeTrue();
        plan.TestKinds.Should().Equal(SolutionPlan.AllTestKinds);
        var prompts = prompter.Confirmations.Select(entry => entry.Prompt).ToList();
        prompts.Should().ContainInOrder(
            "Create the Unit, Integration, EndToEnd test projects?",
            "Create a mock server host (tests/MockServer)?",
            "Install the toolkit's agent skills (.agents/skills) for AI coding agents?");
        prompter.Confirmations.Single(entry => entry.Prompt.Contains("mock server")).DefaultValue.Should().BeFalse();

        var isolated = new ScriptedPrompter(
            "MyApp", root, false, LayeringMode.Folder, "", Array.Empty<object>(), false, false, false, false, false, true);
        new BootstrapWizard(isolated).Run()!.SharedAppSettings.Should().BeFalse();
    }

    [Fact]
    public void Run_TestScaffoldCreatesTheCommonProjectOnceAndHonoursItsPreference()
    {
        var roots = new[] { TempRoot(), TempRoot() };
        try
        {
            var root = roots[0];
            fixture.CopyTo(root);
            Directory.Exists(Path.Combine(root, "tests", "Common")).Should().BeFalse();

            var first = new ScriptedPrompter();
            Application(first, root).Run(["scaffold", "tests", "unit"], interactive: false).Should().Be(0, string.Join("\n", first.Errors));
            first.Progress.Should().Contain("Creating tests/Common/Common.csproj");
            File.ReadAllText(Path.Combine(root, "tests", "Common", "Common.csproj"))
                .Should().Contain("<RootNamespace>MyApp.Tests.Common</RootNamespace>").And.Contain("<IsTestProject>false</IsTestProject>")
                .And.Contain("<PackageReference Include=\"Anarchy.Toolkit.Testing\" />");
            File.Exists(Path.Combine(root, "tests", "Common", "README.md")).Should().BeTrue();
            File.ReadAllText(Path.Combine(root, "tests", "Unit", "Unit.csproj"))
                .Should().Contain("<RootNamespace>MyApp.Tests.Unit</RootNamespace>").And.Contain(@"..\Common\Common.csproj");
            File.ReadAllText(Path.Combine(root, "Directory.Packages.props"))
                .Should().Contain("<PackageVersion Include=\"Anarchy.Toolkit.Testing\" Version=\"6.0.0\" />");
            File.ReadAllText(Path.Combine(root, "MyApp.sln")).Should().Contain(@"tests\Common\Common.csproj").And.Contain(@"tests\Unit\Unit.csproj");

            var second = new ScriptedPrompter();
            Application(second, root).Run(["scaffold", "tests", "integration"], interactive: false).Should().Be(0, string.Join("\n", second.Errors));
            second.Progress.Should().NotContain(step => step.Contains("Common.csproj"));
            File.ReadAllText(Path.Combine(root, "tests", "Integration", "Integration.csproj")).Should().Contain(@"..\Common\Common.csproj");

            var custom = roots[1];
            fixture.CopyTo(custom);
            var configured = new ScriptedPrompter();
            Application(configured, custom).Run(["config", "set", "tests.commonProject", "\"Shared\""], interactive: false)
                .Should().Be(0, string.Join("\n", configured.Errors));
            Application(configured, custom).Run(["scaffold", "tests", "unit"], interactive: false).Should().Be(0, string.Join("\n", configured.Errors));
            File.ReadAllText(Path.Combine(custom, "tests", "Shared", "Shared.csproj")).Should().Contain("<RootNamespace>MyApp.Tests.Shared</RootNamespace>");
            File.ReadAllText(Path.Combine(custom, "tests", "Unit", "Unit.csproj")).Should().Contain(@"..\Shared\Shared.csproj");
            Directory.Exists(Path.Combine(custom, "tests", "Common")).Should().BeFalse();
        }
        finally
        {
            foreach (var root in roots)
                if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
