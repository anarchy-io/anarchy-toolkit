using Anarchy.Toolkit.Automation.Core.Entity;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>Creates the named starting layouts without changing the toolkit's in-code default.</summary>
public static class SolutionLayoutPresets
{
    /// <summary>Creates a preset layout, optionally rooted somewhere other than <c>src</c>.</summary>
    /// <param name="preset">The named layout.</param>
    /// <param name="kind">The application's kind; used by the single-app preset to choose its outer layer.</param>
    /// <param name="applicationFolder">The single-app folder name, defaulting to <c>App</c>.</param>
    /// <param name="root">The checked-project root, defaulting to <c>src</c>.</param>
    /// <returns>The resolved layout.</returns>
    public static SolutionLayout Create(
        SolutionLayoutPreset preset,
        ApplicationKind kind = ApplicationKind.Api,
        string applicationFolder = "App",
        string root = SolutionLayout.DefaultRoot)
    {
        if (!Enum.IsDefined(preset)) throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
        if (preset == SolutionLayoutPreset.Monorepo)
            return new SolutionLayout(LayeringMode.Folder, SolutionLayout.DefaultLayers) {
                Root = root,
                LibraryRoot = $"{root}/{SolutionLayout.LibraryFolder}",
                ApplicationFolders = SolutionLayout.Kinds.Where(applicationKind => SolutionLayout.DefaultLayers.Contains(SolutionLayout.LayerOf(applicationKind)))
                    .ToDictionary(applicationKind => $"{root}/{SolutionLayout.AppsLayer}/{SolutionLayout.FolderOf(applicationKind)}", SolutionLayout.LayerOf)
            };

        var applicationLayer = SolutionLayout.LayerOf(kind);
        var layers = new[] { "Core", "Infrastructure", "Bootstrap", SolutionLayout.RuntimeLayer, applicationLayer };
        return new SolutionLayout(LayeringMode.Folder, layers) {
            Root = root,
            LibraryRoot = root,
            ApplicationFolders = new Dictionary<string, string> { [$"{root}/{applicationFolder}"] = applicationLayer }
        };
    }
}
