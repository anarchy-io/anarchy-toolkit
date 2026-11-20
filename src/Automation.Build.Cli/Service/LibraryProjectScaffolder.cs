using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Automation.Core.Template;
using Anarchy.Toolkit.Automation.Core.Validation;
using Anarchy.Toolkit.Automation.Infrastructure.Helper;

namespace Anarchy.Toolkit.Automation.Build.Cli.Service;

/// <summary>Plans and creates named library projects within an existing solution layer.</summary>
public static class LibraryProjectScaffolder
{
    /// <summary>Resolves the project name and path for a library in the effective solution layout.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the name or layer is invalid, or the path already exists.</exception>
    public static LibraryProjectPlan Plan(SolutionWorkspace workspace, string name, string layer)
    {
        if (!NameRules.IsValidApplicationName(name))
            throw new InvalidOperationException($"'{name}': {NameRules.ApplicationNameHelp}");
        var selectedLayer = workspace.Layout.Layers.FirstOrDefault(value =>
            string.Equals(value, layer, StringComparison.OrdinalIgnoreCase));
        if (selectedLayer is null)
            throw new InvalidOperationException($"Layer '{layer}' is not configured. Choose one of: {string.Join(", ", workspace.Layout.Layers)}.");

        var projectName = workspace.Layout.Mode == LayeringMode.Prefix ? $"{selectedLayer}.{name}" : name;
        if (workspace.ProjectNames.Contains(projectName, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"A project named {projectName} already exists.");

        var root = Normalize(workspace.Layout.Root);
        var libraryRoot = Normalize(workspace.Layout.LibraryRoot ?? $"{root}/{SolutionLayout.LibraryFolder}");
        var relativeDirectory = workspace.Layout.Mode == LayeringMode.Folder
            ? $"{libraryRoot}/{selectedLayer}/{name}"
            : $"{root}/{projectName}";
        var relativeProject = $"{relativeDirectory}/{projectName}.csproj";
        var directory = Path.Combine(workspace.Root, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(directory) || File.Exists(directory) || Directory.Exists(Path.Combine(workspace.Root, relativeProject)) ||
            File.Exists(Path.Combine(workspace.Root, relativeProject)))
            throw new InvalidOperationException($"{relativeDirectory} already exists.");

        var layerIndex = Array.IndexOf(workspace.Layout.Layers.ToArray(), selectedLayer);
        string? reference = null;
        if (layerIndex > 0)
        {
            var lowerPath = Path.Combine(workspace.Root, workspace.Layout.LibraryProjectPath(workspace.Layout.Layers[layerIndex - 1])
                .Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(lowerPath))
                reference = Path.GetRelativePath(directory, lowerPath).Replace('\\', '/');
        }

        return new LibraryProjectPlan(projectName, relativeDirectory, relativeProject, directory, reference);
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');

    /// <summary>Creates a planned library and adds it to the solution, or reports it without writing in dry-run mode.</summary>
    public static void Add(SolutionWorkspace workspace, LibraryProjectPlan plan, bool dryRun, Action<string>? progress = null)
    {
        var report = progress ?? (_ => { });
        if (dryRun)
        {
            report("Dry run: nothing is written.");
            report($"Create: {plan.RelativeProjectPath}");
            report($"Add {plan.RelativeProjectPath} to {Path.GetFileName(workspace.SolutionFile)}");
            return;
        }

        report($"Creating {plan.RelativeProjectPath}");
        FileHelper.WriteNewText(Path.Combine(plan.Directory, Path.GetFileName(plan.RelativeProjectPath)),
            ApplicationTemplates.Library(plan.LowerLayerReference));
        report($"Adding it to {Path.GetFileName(workspace.SolutionFile)}");
        DotNetCli.Run(workspace.Root, "sln", workspace.SolutionFile, "add", plan.RelativeProjectPath);
    }
}

/// <summary>The resolved output paths and lower-layer dependency of a named library project.</summary>
/// <param name="ProjectName">The project and assembly name.</param>
/// <param name="RelativeDirectory">The solution-relative project directory.</param>
/// <param name="RelativeProjectPath">The solution-relative project file.</param>
/// <param name="Directory">The absolute project directory.</param>
/// <param name="LowerLayerReference">The project reference to the configured lower layer, if it exists.</param>
public sealed record LibraryProjectPlan(
    string ProjectName,
    string RelativeDirectory,
    string RelativeProjectPath,
    string Directory,
    string? LowerLayerReference);
