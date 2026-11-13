using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Template;
using Anarchy.Toolkit.Build.Cli.Core.Validation;
using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>
/// Bootstraps a whole solution into an empty directory: the Fallout build host, the standard scaffold, a library
/// project per layer and the planned applications, all added to the solution, with layering configured for the layout.
/// </summary>
public static class SolutionBootstrapper
{
    /// <summary>Why <paramref name="directory"/> cannot receive a new solution, or null if it can.</summary>
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

    /// <summary>Why the plan cannot be created, or null if it can.</summary>
    public static string? Validate(SolutionPlan plan)
    {
        if (!NameRules.IsValidSolutionName(plan.Name)) return NameRules.SolutionNameHelp;
        if (NameRules.ValidateLayers(plan.Layout.Layers) is { } layers) return layers;

        var taken = plan.Layout.Layers.ToList();
        foreach (var app in plan.Applications)
        {
            if (ApplicationScaffolder.Validate(plan.Layout, app, taken) is { } error) return error;
            taken.Add(plan.Layout.ProjectName(app));
        }
        return ValidateRootDirectory(plan.RootDirectory);
    }

    public static void Create(SolutionPlan plan, ToolkitPackageVersions versions, Action<string>? progress = null)
    {
        if (Validate(plan) is { } error) throw new InvalidOperationException(error);
        var report = progress ?? (_ => { });
        var root = Path.GetFullPath(plan.RootDirectory);
        var layout = plan.Layout;
        var packageVersions = plan.Applications.Select(app => app.PackageId).Distinct()
            .ToDictionary(id => id, id => versions[id]);

        report("Creating the build host");
        BuildHostScaffolder.Create(root, versions[ToolkitPackageVersions.BuildFallout]);

        report("Installing the scaffold and creating the solution");
        SolutionScaffolder.Create(root, plan.Name, new ScaffoldOptions { Exclude = "core" });
        FalloutParameters.Add(root, layout.ToFalloutParameters());

        report($"Creating the {string.Join(", ", layout.Layers)} libraries");
        string? lower = null;
        foreach (var layer in layout.Layers)
        {
            var path = Path.Combine(root, layout.LibraryProjectPath(layer));
            var reference = lower is null ? null : Path.GetRelativePath(Path.GetDirectoryName(path)!, lower).Replace('\\', '/');
            FileHelper.WriteNewText(path, ApplicationTemplates.Library(reference));
            lower = path;
        }

        var ports = plan.Ports;
        foreach (var app in plan.Applications)
        {
            report($"Creating {layout.ProjectPath(app)}");
            ApplicationScaffolder.Create(root, layout, app, packageVersions[app.PackageId], ports.TryGetValue(app, out var port) ? port : null);
        }

        report("Adding the projects to the solution");
        DotNetCli.Run(root, [
            "sln", plan.Name + ".sln", "add",
            .. layout.Layers.Select(layout.LibraryProjectPath),
            .. plan.Applications.Select(layout.ProjectPath)
        ]);
    }
}
