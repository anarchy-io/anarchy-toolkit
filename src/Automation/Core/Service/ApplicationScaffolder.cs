using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Template;
using Anarchy.Toolkit.Automation.Core.Validation;
using Anarchy.Toolkit.Automation.Helpers.DotNet;
using Anarchy.Toolkit.Automation.Helpers.Files;
using Anarchy.Toolkit.Automation.Helpers.Packages;
using Anarchy.Toolkit.Core.Helpers.Ports;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// Writes application projects into a solution's layout.
/// </summary>
/// <remarks>
/// Used by <see cref="SolutionBootstrapper"/> for new solutions and directly (through <see cref="Add"/>) for existing
/// ones. Project files come from <see cref="Anarchy.Toolkit.Automation.Core.Template.ApplicationTemplates"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var workspace = SolutionWorkspace.Find(Directory.GetCurrentDirectory())!;
/// ApplicationScaffolder.Add(workspace, new ApplicationSpec(ApplicationKind.Api, "Orders"), versions, Console.WriteLine);
/// </code>
/// </example>
public static class ApplicationScaffolder
{
    /// <summary>
    /// Why <paramref name="app"/> cannot join a solution with the <paramref name="taken"/> project names, or null if it can.
    /// </summary>
    /// <param name="layout">The solution's layout.</param>
    /// <param name="app">The application to add.</param>
    /// <param name="taken">The project names already in the solution (compared case-insensitively).</param>
    /// <returns>An error message, or <see langword="null"/> when the application is valid.</returns>
    public static string? Validate(SolutionLayout layout, ApplicationSpec app, IEnumerable<string> taken)
    {
        if (!NameRules.IsValidApplicationName(app.Name)) return $"'{app.Name}': {NameRules.ApplicationNameHelp}";
        if (app.Container && !app.IsWebHost) return "Container web hosting is only available for APIs and UIs.";
        if (app.ApiLambda && app.Kind != ApplicationKind.Api) return "API Gateway Lambda is only available for APIs.";
        if (app.EventLambda && app.Kind != ApplicationKind.Job) return "Event Lambda is only available for jobs.";
        if (!Enum.IsDefined(app.JobMode)) return "Invalid job execution mode.";
        if (!layout.Layers.Contains(SolutionLayout.LayerOf(app.Kind)))
            return $"A {app.Kind} belongs to the {SolutionLayout.LayerOf(app.Kind)} layer, which the solution does not have.";
        if (!layout.Supports(app.Kind)) return layout.MissingApplicationFolder(app.Kind);
        var project = layout.ProjectName(app);
        return taken.Contains(project, StringComparer.OrdinalIgnoreCase) ? $"A project named {project} already exists." : null;
    }

    /// <summary>
    /// Writes the project; adding it to the solution is up to the caller. Follows the solution's package management:
    /// under a <c>Directory.Packages.props</c> enabling central management, the package version is pinned there (unless
    /// it already is) and the reference is unversioned; otherwise the reference carries the version. The project
    /// references its layer's library only if that library exists.
    /// </summary>
    /// <param name="root">The solution directory.</param>
    /// <param name="layout">The solution's layout.</param>
    /// <param name="app">The application to create.</param>
    /// <param name="versions">The toolkit package versions; only those of <see cref="ApplicationSpec.PackageIds"/> are used.</param>
    /// <param name="ports">The local HTTP and HTTPS ports of a web host, or <see langword="null"/> for the defaults.</param>
    /// <param name="solutionName">The solution name, prefixing the project's root namespace.</param>
    /// <param name="sharedSettings">
    /// The libraries holding the solution's shared settings, or <see langword="null"/> for the application's own.
    /// </param>
    /// <exception cref="System.InvalidOperationException">Thrown when the project directory already exists.</exception>
    public static void Create(string root, SolutionLayout layout, ApplicationSpec app, ToolkitPackageVersions versions, (int Http, int Https)? ports,
        string solutionName, SharedAppSettings? sharedSettings = null)
    {
        root = Path.GetFullPath(root);
        var directory = Path.Combine(root, layout.ProjectDirectory(app));
        if (Directory.Exists(directory) || File.Exists(directory))
            throw new InvalidOperationException($"{directory} already exists.");

        var library = layout.LibraryOf(app.Kind) is { } path && File.Exists(Path.Combine(root, path))
            ? Path.GetRelativePath(directory, Path.Combine(root, path)).Replace('\\', '/')
            : null;
        var central = CentralPackages.Find(directory);
        if (central is not null)
            foreach (var packageId in app.PackageIds) CentralPackages.Ensure(central, packageId, versions[packageId]);
        var context = new ApplicationContext(app, layout.ProjectName(app), layout.RootNamespace(app, solutionName),
            central is null ? versions[app.PackageId] : null, ports, library) {
            SwaggerPackageVersion = central is null && app.HasSwagger ? versions[ApplicationSpec.SwaggerPackageId] : null,
            LayeringMode = layout.Mode,
            SharedSettings = sharedSettings
        };
        foreach (var (file, content) in ApplicationTemplates.Render(context))
            FileHelper.WriteNewText(Path.Combine(directory, file), content);
    }

    /// <summary>
    /// The preference choosing between shared and isolated application settings.
    /// </summary>
    public const string SharedSettingsPreference = "scaffolding.appSettings";

    /// <summary>
    /// The <see cref="SharedSettingsPreference"/> value of shared settings.
    /// </summary>
    public const string SharedSettingsValue = "shared";

    /// <summary>
    /// The shared settings of a solution, whose Infrastructure library must hold the shared <c>AppSettings</c>.
    /// </summary>
    /// <param name="root">The solution directory.</param>
    /// <param name="layout">The solution's layout.</param>
    /// <param name="solutionName">The solution name.</param>
    /// <returns>The libraries' root namespaces.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when the layout or the solution has no shared settings.</exception>
    public static SharedAppSettings SharedSettingsOf(string root, SolutionLayout layout, string solutionName)
    {
        if (!layout.SupportsSharedAppSettings)
            throw new InvalidOperationException($"{SharedSettingsPreference} is shared, but the layout has no separate {SolutionLayout.InfrastructureLayer} and {SolutionLayout.BootstrapLayer} libraries.");
        var infrastructure = layout.LibraryProjectPath(SolutionLayout.InfrastructureLayer);
        var settings = Path.Combine(root, infrastructure[..infrastructure.LastIndexOf('/')], SharedAppSettingsTemplates.AppSettingsPath);
        if (!File.Exists(settings))
            throw new InvalidOperationException($"{SharedSettingsPreference} is shared, but {Path.GetRelativePath(root, settings)} does not exist; create it or set {SharedSettingsPreference} to isolated.");
        return new SharedAppSettings(layout.LibraryNamespace(SolutionLayout.InfrastructureLayer, solutionName),
            layout.LibraryNamespace(SolutionLayout.BootstrapLayer, solutionName));
    }

    /// <summary>
    /// Adds an application to an existing solution, on the next free local ports.
    /// </summary>
    /// <param name="workspace">The opened solution.</param>
    /// <param name="app">The application to add.</param>
    /// <param name="versions">The toolkit package versions.</param>
    /// <param name="progress">Receives progress messages; <see langword="null"/> discards them.</param>
    /// <exception cref="System.InvalidOperationException">Thrown when the application is invalid for the solution (see <see cref="Validate"/>), or when its directory already exists.</exception>
    public static void Add(SolutionWorkspace workspace, ApplicationSpec app, ToolkitPackageVersions versions, Action<string>? progress = null)
    {
        if (Validate(workspace.Layout, app, workspace.ProjectNames) is { } error) throw new InvalidOperationException(error);
        var report = progress ?? (_ => { });
        var ports = LocalPorts.Assign(app.IsWebHost ? [app] : Array.Empty<ApplicationSpec>(), workspace.UsedPorts);

        var solutionName = Path.GetFileNameWithoutExtension(workspace.SolutionFile);
        var shared = AutomationPreferences.Read(workspace.Root).Get<string>(SharedSettingsPreference) == SharedSettingsValue
            ? SharedSettingsOf(workspace.Root, workspace.Layout, solutionName)
            : null;

        report($"Creating {workspace.Layout.ProjectPath(app)}");
        Create(workspace.Root, workspace.Layout, app, versions, ports.TryGetValue(app, out var port) ? port : null, solutionName, shared);

        report($"Adding it to {Path.GetFileName(workspace.SolutionFile)}");
        DotNetCli.Run(workspace.Root, "sln", workspace.SolutionFile, "add", workspace.Layout.ProjectPath(app));
    }
}
