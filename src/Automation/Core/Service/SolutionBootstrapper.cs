using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Template;
using Anarchy.Toolkit.Automation.Core.Validation;
using Anarchy.Toolkit.Automation.Helpers.DotNet;
using Anarchy.Toolkit.Automation.Helpers.Files;
using Anarchy.Toolkit.Automation.Helpers.Packages;
using Anarchy.Toolkit.Automation.Helpers.Solutions;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// Bootstraps a whole solution into an empty directory: the Anarchy build host (<c>build/_build.csproj</c>, where the
/// solution customizes its automation), the standard scaffold, a library project per layer but <c>Apps</c>, the planned
/// applications and test projects, all added to the solution, with the solution-level files under a
/// <c>solution items</c> folder. No <c>samples</c> folder is created. Test projects reference a shared, non-executable
/// common test project; a mock server host can be added to <c>tests/MockServer</c>; and the applications can share
/// one <c>AppSettings</c>, which is then recorded as the <c>scaffolding.appSettings</c> preference. Preferences are saved only when the
/// caller explicitly requests it.
/// Package versions are always managed centrally, in the root <c>Directory.Packages.props</c>.
/// </summary>
/// <example>
/// <code language="csharp">
/// var plan = new SolutionPlan("Shop", "/work/shop", SolutionLayout.Default,
///     [new ApplicationSpec(ApplicationKind.Api, "Orders")]);
/// if (SolutionBootstrapper.Validate(plan) is { } error) Console.Error.WriteLine(error);
/// else SolutionBootstrapper.Create(plan, versions, Console.WriteLine);
/// </code>
/// </example>
public static class SolutionBootstrapper
{
    /// <summary>
    /// The solution-level files and directories listed in the <see cref="SolutionItems.FolderName"/> solution folder,
    /// when they exist.
    /// </summary>
    public static IReadOnlyList<string> SolutionItemPaths { get; } = [
        "scripts", "Makefile", AutomationPreferences.FileName, "global.json", "nuget.config", "Directory.Build.props",
        CentralPackages.FileName, ".editorconfig"
    ];

    /// <summary>
    /// Why <paramref name="directory"/> cannot receive a new solution, or null if it can.
    /// </summary>
    /// <param name="directory">The directory to check; it must be missing or empty.</param>
    /// <returns>An error message, or <see langword="null"/>.</returns>
    public static string? ValidateRootDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return "Enter a directory.";
        try
        {
            return FileHelper.IsMissingOrEmptyDirectory(Path.GetFullPath(directory))
                ? null
                : "The directory must be empty or not yet exist.";
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            return $"Invalid directory: {exception.Message}";
        }
    }

    /// <summary>
    /// Why the plan cannot be created, or null if it can.
    /// </summary>
    /// <remarks>
    /// Checks the solution name, the layers, the layout's folders (see <see cref="LayeringConfiguration.Validate"/>),
    /// every application and the root directory, in that order.
    /// </remarks>
    /// <param name="plan">The plan to check.</param>
    /// <returns>The first error message, or <see langword="null"/>.</returns>
    public static string? Validate(SolutionPlan plan)
    {
        if (!NameRules.IsValidSolutionName(plan.Name)) return NameRules.SolutionNameHelp;
        if (NameRules.ValidateLayers(plan.Layout.Layers) is { } layers) return layers;
        if (LayeringConfiguration.Validate(plan.Layout) is [var layout, ..]) return $"Invalid layering configuration: {layout}.";
        if (plan.TestKinds.FirstOrDefault(kind => !SolutionPlan.AllTestKinds.Contains(kind, StringComparer.Ordinal)) is { } test)
            return $"Unknown test kind '{test}': use {string.Join(", ", SolutionPlan.AllTestKinds)}.";
        if (!NameRules.IsValidSolutionName(plan.CommonTestProject) ||
            SolutionPlan.AllTestKinds.Append(TestTemplates.MockServerProject).Contains(plan.CommonTestProject, StringComparer.OrdinalIgnoreCase))
            return $"'{plan.CommonTestProject}' cannot name the common test project.";
        if (plan.SharedAppSettings && !plan.Layout.SupportsSharedAppSettings)
            return $"Shared settings need separate {SolutionLayout.InfrastructureLayer} and {SolutionLayout.BootstrapLayer} libraries.";

        var taken = plan.Layout.Layers.ToList();
        foreach (var app in plan.Applications)
        {
            if (ApplicationScaffolder.Validate(plan.Layout, app, taken) is { } error) return error;
            taken.Add(plan.Layout.ProjectName(app));
        }
        return ValidateRootDirectory(plan.RootDirectory);
    }

    /// <summary>
    /// Creates the planned solution, reporting each step to <paramref name="progress"/>.
    /// </summary>
    /// <param name="plan">The solution to create.</param>
    /// <param name="versions">The toolkit package versions, pinned in <c>Directory.Packages.props</c>.</param>
    /// <param name="progress">Receives progress messages; <see langword="null"/> discards them.</param>
    /// <exception cref="System.InvalidOperationException">Thrown when the plan is invalid (see <see cref="Validate"/>) or a <c>dotnet</c> command fails.</exception>
    public static void Create(SolutionPlan plan, ToolkitPackageVersions versions, Action<string>? progress = null)
    {
        if (Validate(plan) is { } error) throw new InvalidOperationException(error);
        var report = progress ?? (_ => { });
        var root = Path.GetFullPath(plan.RootDirectory);
        var layout = plan.Layout;
        var hasTests = plan.TestKinds.Count > 0;
        var commonPackages = new List<string> { TestTemplates.TestingPackage };
        if (plan.Applications.Any(app => app.IsWebHost)) commonPackages.Add(TestTemplates.TestingWebPackage);
        if (plan.MockServer) commonPackages.Add(TestTemplates.TestingMockServerPackage);
        var packageVersions = plan.Applications.SelectMany(app => app.PackageIds).Append(ToolkitPackageVersions.AutomationBuild)
            .Concat(plan.SharedAppSettings ? [SharedAppSettingsTemplates.PackageId] : [])
            .Concat(hasTests ? commonPackages : [])
            .Concat(plan.MockServer ? [TestTemplates.MockServerPackage] : [])
            .Distinct().ToDictionary(id => id, id => versions[id]);

        report("Creating the central package versions");
        FileHelper.WriteNewText(Path.Combine(root, CentralPackages.FileName), CentralPackages.Render(packageVersions));

        report("Creating the build host");
        BuildHostScaffolder.Create(root, null);

        report("Installing the scaffold and creating the solution");
        SolutionScaffolder.Create(root, plan.Name, new ScaffoldOptions { Items = plan.AgentSkills ? "all,skills" : null, Exclude = "core,samples" });
        report($"Creating the {string.Join(", ", layout.LibraryLayers)} libraries");
        var shared = plan.SharedAppSettings
            ? new SharedAppSettings(layout.LibraryNamespace(SolutionLayout.InfrastructureLayer, plan.Name),
                layout.LibraryNamespace(SolutionLayout.BootstrapLayer, plan.Name))
            : null;
        string? lower = null;
        foreach (var layer in layout.LibraryLayers)
        {
            var path = Path.Combine(root, layout.LibraryProjectPath(layer));
            var directory = Path.GetDirectoryName(path)!;
            var reference = lower is null ? null : Path.GetRelativePath(directory, lower).Replace('\\', '/');
            var files = shared is null ? null : layer switch {
                SolutionLayout.InfrastructureLayer => SharedAppSettingsTemplates.Infrastructure(shared),
                SolutionLayout.BootstrapLayer => SharedAppSettingsTemplates.Bootstrap(shared),
                _ => null
            };
            FileHelper.WriteNewText(path, ApplicationTemplates.Library(reference, layout.LibraryNamespace(layer, plan.Name),
                files is null ? null : [(SharedAppSettingsTemplates.PackageId, null)]));
            foreach (var (file, content) in files ?? new Dictionary<string, string>())
                FileHelper.WriteNewText(Path.Combine(directory, file), content);
            lower = path;
        }

        if (shared is not null)
        {
            report("Sharing the settings through Directory.Build.props and .anarchy-toolkit.json");
            var props = Path.Combine(root, "Directory.Build.props");
            var content = File.ReadAllText(props);
            var close = content.LastIndexOf("</Project>", StringComparison.Ordinal);
            File.WriteAllText(props, content[..close] + SharedAppSettingsTemplates.DirectoryBuildProps(layout) + content[close..]);
            AutomationPreferences.Read(root)
                .With(ApplicationScaffolder.SharedSettingsPreference, $"\"{ApplicationScaffolder.SharedSettingsValue}\"")
                .Save(root);
        }

        var ports = plan.Ports;
        foreach (var app in plan.Applications)
        {
            report($"Creating {layout.ProjectPath(app)}");
            ApplicationScaffolder.Create(root, layout, app, versions, ports.TryGetValue(app, out var port) ? port : null,
                plan.Name, shared);
        }

        var tests = new List<string>();
        if (hasTests)
        {
            var common = SolutionPlan.TestProjectPath(plan.CommonTestProject);
            report($"Creating {common}");
            foreach (var (file, content) in TestTemplates.Common(plan.CommonTestProject, SolutionLayout.Namespace(plan.Name, $"tests/{plan.CommonTestProject}"),
                         commonPackages.Select(id => (id, (string?)null))))
                FileHelper.WriteNewText(Path.Combine(root, "tests", plan.CommonTestProject, file), content);
            tests.Add(common);
            foreach (var kind in plan.TestKinds.Distinct(StringComparer.Ordinal))
            {
                var test = SolutionPlan.TestProjectPath(kind);
                report($"Creating {test}");
                FileHelper.WriteNewText(Path.Combine(root, test), TestTemplates.TestProject(SolutionLayout.Namespace(plan.Name, $"tests/{kind}"),
                    $"../{plan.CommonTestProject}/{plan.CommonTestProject}.csproj"));
                tests.Add(test);
            }
        }
        if (plan.MockServer)
        {
            var mockServer = SolutionPlan.TestProjectPath(TestTemplates.MockServerProject);
            report($"Creating {mockServer}");
            foreach (var (file, content) in TestTemplates.MockServer(SolutionLayout.Namespace(plan.Name, $"tests/{TestTemplates.MockServerProject}"), null))
                FileHelper.WriteNewText(Path.Combine(root, "tests", TestTemplates.MockServerProject, file), content);
            tests.Add(mockServer);
        }

        report("Adding the projects to the solution");
        DotNetCli.Run(root, [
            "sln", plan.Name + ".sln", "add",
            .. layout.LibraryLayers.Select(layout.LibraryProjectPath),
            .. plan.Applications.Select(layout.ProjectPath),
            .. tests
        ]);

        report($"Listing the solution items in the '{SolutionItems.FolderName}' folder");
        SolutionItems.Add(root, plan.Name + ".sln", SolutionItemPaths);
    }
}
