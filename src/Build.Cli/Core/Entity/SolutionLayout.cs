namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// Where a solution's projects live and the layers they are checked against, as configured in Fallout's
/// <c>.fallout/parameters.json</c> (see <c>ILayeringBuild</c>).
/// </summary>
public sealed record SolutionLayout(LayeringMode Mode, IReadOnlyList<string> Layers)
{
    public const string LibraryFolder = "Library";
    public const string WebLayer = "Web";
    public const string HostingLayer = "Hosting";

    /// <summary>
    /// The layers of a new solution, innermost first.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultLayers = ["Core", "Infrastructure", "Configuration", HostingLayer, WebLayer];

    /// <summary>
    /// The layout of a new solution: <see cref="LayeringMode.Folder"/> over the <see cref="DefaultLayers"/>.
    /// </summary>
    public static SolutionLayout Default { get; } = new(LayeringMode.Folder, DefaultLayers);

    /// <summary>
    /// Every <see cref="ApplicationKind"/>, in the order their application folders are configured.
    /// </summary>
    public static IReadOnlyList<ApplicationKind> Kinds { get; } = [ApplicationKind.Api, ApplicationKind.Job, ApplicationKind.Ui];

    /// <summary>
    /// The layer an application belongs to.
    /// </summary>
    public static string LayerOf(ApplicationKind kind) => kind == ApplicationKind.Job ? HostingLayer : WebLayer;

    /// <summary>
    /// The folder under <c>src</c> that holds the applications of a kind in <see cref="LayeringMode.Folder"/>.
    /// </summary>
    public static string FolderOf(ApplicationKind kind) => kind switch {
        ApplicationKind.Api => "Api",
        ApplicationKind.Job => "Jobs",
        ApplicationKind.Ui => "Ui",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static string PrefixOf(ApplicationKind kind) => kind switch {
        ApplicationKind.Api => "Api",
        ApplicationKind.Job => "Job",
        ApplicationKind.Ui => "Ui",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// Whether this layout has the layer applications of a kind belong to.
    /// </summary>
    public bool Supports(ApplicationKind kind) => Layers.Contains(LayerOf(kind));

    /// <summary>
    /// The library project file of a layer, relative to the solution directory.
    /// </summary>
    public string LibraryProjectPath(string layer) => Mode == LayeringMode.Folder
        ? $"src/{LibraryFolder}/{layer}/{layer}.csproj"
        : $"src/{layer}/{layer}.csproj";

    /// <summary>
    /// The application's project name: its plain name in <see cref="LayeringMode.Folder"/>, otherwise prefixed with
    /// its layer and kind (e.g. <c>Web.Api.Orders</c>).
    /// </summary>
    public string ProjectName(ApplicationSpec app) => Mode == LayeringMode.Folder
        ? app.Name
        : $"{LayerOf(app.Kind)}.{PrefixOf(app.Kind)}.{app.Name}";

    /// <summary>
    /// The application's root namespace (and assembly name): its folder and name in <see cref="LayeringMode.Folder"/>
    /// (e.g. <c>Api.Orders</c>), otherwise its <see cref="ProjectName"/>.
    /// </summary>
    public string RootNamespace(ApplicationSpec app) => Mode == LayeringMode.Folder
        ? $"{FolderOf(app.Kind)}.{app.Name}"
        : ProjectName(app);

    /// <summary>
    /// The project directory, relative to the solution directory.
    /// </summary>
    public string ProjectDirectory(ApplicationSpec app) => Mode == LayeringMode.Folder
        ? $"src/{FolderOf(app.Kind)}/{app.Name}"
        : $"src/{ProjectName(app)}";

    /// <summary>
    /// The project file, relative to the solution directory.
    /// </summary>
    public string ProjectPath(ApplicationSpec app) => $"{ProjectDirectory(app)}/{ProjectName(app)}.csproj";

    /// <summary>
    /// The library an application references: its own layer's, or null if the layout has no such layer.
    /// </summary>
    public string? LibraryOf(ApplicationKind kind) =>
        Supports(kind) ? LibraryProjectPath(LayerOf(kind)) : null;

    /// <summary>
    /// The <c>ILayeringBuild</c> parameters that check this layout.
    /// </summary>
    public IReadOnlyDictionary<string, object> ToFalloutParameters() => Mode == LayeringMode.Folder
        ? new Dictionary<string, object> {
            ["LayeringLayers"] = Layers.ToArray(),
            ["LayeringByFolder"] = true,
            ["LayeringRoot"] = $"src/{LibraryFolder}",
            ["LayeringApplicationFolders"] = Kinds.Select(kind => $"src/{FolderOf(kind)}={LayerOf(kind)}").ToArray()
        }
        : new Dictionary<string, object> {
            ["LayeringLayers"] = Layers.ToArray(),
            ["LayeringByFolder"] = false,
            ["LayeringRoot"] = "src"
        };
}
