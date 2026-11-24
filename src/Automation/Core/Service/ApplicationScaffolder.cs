using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Template;
using Anarchy.Toolkit.Automation.Core.Validation;
using Anarchy.Toolkit.Automation.Infrastructure.Helper;
using Anarchy.Toolkit.Core.Helper;

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
    /// <param name="packageVersion">The version of the application's toolkit package.</param>
    /// <param name="ports">The local HTTP and HTTPS ports of a web host, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="System.InvalidOperationException">Thrown when the project directory already exists.</exception>
    public static void Create(string root, SolutionLayout layout, ApplicationSpec app, string packageVersion, (int Http, int Https)? ports)
    {
        root = Path.GetFullPath(root);
        var directory = Path.Combine(root, layout.ProjectDirectory(app));
        if (Directory.Exists(directory) || File.Exists(directory))
            throw new InvalidOperationException($"{directory} already exists.");

        var library = layout.LibraryOf(app.Kind) is { } path && File.Exists(Path.Combine(root, path))
            ? Path.GetRelativePath(directory, Path.Combine(root, path)).Replace('\\', '/')
            : null;
        string? referenceVersion = packageVersion;
        if (CentralPackages.Find(directory) is { } centralPackages)
        {
            CentralPackages.Ensure(centralPackages, app.PackageId, packageVersion);
            referenceVersion = null;
        }
        var context = new ApplicationContext(app, layout.ProjectName(app), layout.RootNamespace(app), referenceVersion, ports, library) { LayeringMode = layout.Mode };
        foreach (var (file, content) in ApplicationTemplates.Render(context))
            FileHelper.WriteNewText(Path.Combine(directory, file), content);
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

        report($"Creating {workspace.Layout.ProjectPath(app)}");
        Create(workspace.Root, workspace.Layout, app, versions[app.PackageId], ports.TryGetValue(app, out var port) ? port : null);

        report($"Adding it to {Path.GetFileName(workspace.SolutionFile)}");
        DotNetCli.Run(workspace.Root, "sln", workspace.SolutionFile, "add", workspace.Layout.ProjectPath(app));
    }
}
