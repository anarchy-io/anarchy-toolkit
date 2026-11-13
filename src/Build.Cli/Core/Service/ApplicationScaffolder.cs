using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Template;
using Anarchy.Toolkit.Build.Cli.Core.Validation;
using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>Writes application projects into a solution's layout.</summary>
public static class ApplicationScaffolder
{
    /// <summary>Why <paramref name="app"/> cannot join a solution with the <paramref name="taken"/> project names, or null if it can.</summary>
    public static string? Validate(SolutionLayout layout, ApplicationSpec app, IEnumerable<string> taken)
    {
        if (!NameRules.IsValidApplicationName(app.Name)) return $"'{app.Name}': {NameRules.ApplicationNameHelp}";
        if (!layout.Supports(app.Kind))
            return $"A {app.Kind} belongs to the {SolutionLayout.LayerOf(app.Kind)} layer, which the solution does not have.";
        var project = layout.ProjectName(app);
        return taken.Contains(project, StringComparer.OrdinalIgnoreCase) ? $"A project named {project} already exists." : null;
    }

    /// <summary>Writes the project; adding it to the solution is up to the caller.</summary>
    public static void Create(string root, SolutionLayout layout, ApplicationSpec app, string packageVersion, (int Http, int Https)? ports)
    {
        root = Path.GetFullPath(root);
        var directory = Path.Combine(root, layout.ProjectDirectory(app));
        if (Directory.Exists(directory) || File.Exists(directory))
            throw new InvalidOperationException($"{directory} already exists.");

        var library = layout.LibraryOf(app.Kind) is { } path && File.Exists(Path.Combine(root, path))
            ? Path.GetRelativePath(directory, Path.Combine(root, path)).Replace('\\', '/')
            : null;
        var context = new ApplicationContext(app, layout.ProjectName(app), layout.RootNamespace(app), packageVersion, ports, library);
        foreach (var (file, content) in ApplicationTemplates.Render(context))
            FileHelper.WriteNewText(Path.Combine(directory, file), content);
    }

    /// <summary>Adds an application to an existing solution, on the next free local ports.</summary>
    public static void Add(SolutionWorkspace workspace, ApplicationSpec app, ToolkitPackageVersions versions, Action<string>? progress = null)
    {
        if (Validate(workspace.Layout, app, workspace.ProjectNames) is { } error) throw new InvalidOperationException(error);
        var report = progress ?? (_ => { });
        var ports = LocalPorts.Assign([app], workspace.UsedPorts);

        report($"Creating {workspace.Layout.ProjectPath(app)}");
        Create(workspace.Root, workspace.Layout, app, versions[app.PackageId], ports.TryGetValue(app, out var port) ? port : null);

        report($"Adding it to {Path.GetFileName(workspace.SolutionFile)}");
        DotNetCli.Run(workspace.Root, "sln", workspace.SolutionFile, "add", workspace.Layout.ProjectPath(app));
    }
}
