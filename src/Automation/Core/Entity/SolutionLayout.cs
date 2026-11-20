namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>
/// Where a solution's projects live and the layers they are checked against, as configured in the solution's
/// optional <c>.anarchy-toolkit.json</c> (see <see cref="Anarchy.Toolkit.Automation.Core.Service.LayeringConfiguration"/>).
/// </summary>
/// <remarks>
/// The layout is the single source of truth for where the scaffolder puts library and application projects, how they
/// are named and namespaced, and how <c>CheckLayering</c> and <c>GenerateDependencyMap</c> find each project's layer.
/// Its folder settings (<see cref="LibraryRoot"/> and <see cref="ApplicationFolders"/>) only apply to
/// <see cref="LayeringMode.Folder"/>; a <see cref="LayeringMode.Prefix"/> layout must leave them unset.
/// </remarks>
/// <param name="Mode">How projects are placed and how their layer is determined.</param>
/// <param name="Layers">The layers, innermost first.</param>
public sealed record SolutionLayout(LayeringMode Mode, IReadOnlyList<string> Layers)
{
    /// <summary>
    /// The folder under <see cref="Root"/> that holds the layer libraries of a new solution in <see cref="LayeringMode.Folder"/>.
    /// </summary>
    public const string LibraryFolder = "Library";

    /// <summary>
    /// The default <see cref="Root"/>: <c>src</c>.
    /// </summary>
    public const string DefaultRoot = "src";

    /// <summary>
    /// The layer APIs and UIs belong to.
    /// </summary>
    public const string WebLayer = "Web";

    /// <summary>
    /// The layer jobs belong to.
    /// </summary>
    public const string HostingLayer = "Hosting";

    /// <summary>
    /// The layers a new solution starts with, innermost first: one library project is created for each.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultLayers =
        ["Core", "Infrastructure", "Configuration", HostingLayer, WebLayer, "Automation"];

    /// <summary>
    /// Every <see cref="ApplicationKind"/>, in the order their application folders are configured.
    /// </summary>
    public static IReadOnlyList<ApplicationKind> Kinds { get; } = [ApplicationKind.Api, ApplicationKind.Job, ApplicationKind.Ui];

    /// <summary>
    /// The layout of a new solution: <see cref="LayeringMode.Folder"/> over the <see cref="DefaultLayers"/>.
    /// </summary>
    public static SolutionLayout Default { get; } = new(LayeringMode.Folder, DefaultLayers);

    /// <summary>
    /// The solution-relative folder holding every layered project; projects outside it (tests, samples, the build
    /// project) are not checked. Defaults to <see cref="DefaultRoot"/>.
    /// </summary>
    public string Root { get; init; } = DefaultRoot;

    /// <summary>
    /// In <see cref="LayeringMode.Folder"/>, the solution-relative folder whose first-level folders are the layers
    /// (e.g. <c>src/Library</c>, holding <c>Core/</c>, <c>Infrastructure/</c>, ...). Defaults to
    /// <c>src/Library</c> in folder mode and is <see langword="null"/> in prefix mode.
    /// </summary>
    public string? LibraryRoot { get; init; } = Mode == LayeringMode.Folder ? $"{DefaultRoot}/{LibraryFolder}" : null;

    /// <summary>
    /// In <see cref="LayeringMode.Folder"/>, the solution-relative folders whose projects all belong to one layer
    /// (e.g. <c>src/Api</c> → <c>Web</c>), checked before <see cref="LibraryRoot"/>. Defaults to one folder per
    /// <see cref="Kinds">application kind</see> whose layer is in <see cref="Layers"/> in folder mode, and is empty in
    /// prefix mode.
    /// </summary>
    public IReadOnlyDictionary<string, string> ApplicationFolders { get; init; } = Mode == LayeringMode.Folder
        ? Kinds.Where(kind => Layers.Contains(LayerOf(kind)))
            .ToDictionary(kind => $"{DefaultRoot}/{FolderOf(kind)}", LayerOf)
        : new Dictionary<string, string>();

    /// <summary>
    /// The layer an application belongs to.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns><see cref="HostingLayer"/> for a job, otherwise <see cref="WebLayer"/>.</returns>
    public static string LayerOf(ApplicationKind kind) => kind == ApplicationKind.Job ? HostingLayer : WebLayer;

    /// <summary>
    /// The name of the application folder that holds the applications of a kind in <see cref="LayeringMode.Folder"/>.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns><c>Api</c>, <c>Jobs</c> or <c>Ui</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="kind"/> is not a known value.</exception>
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
    /// In <see cref="LayeringMode.Folder"/>, the configured application folder that receives applications of a kind:
    /// the one named <see cref="FolderOf"/> (e.g. <c>src/Api</c>) and mapped to <see cref="LayerOf"/>.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns>The solution-relative folder, or <see langword="null"/> in prefix mode or when none is configured.</returns>
    public string? ApplicationFolderOf(ApplicationKind kind)
    {
        if (Mode != LayeringMode.Folder) return null;
        var standard = ApplicationFolders
            .Where(entry => entry.Value == LayerOf(kind) &&
                            string.Equals(entry.Key.Replace('\\', '/').TrimEnd('/').Split('/')[^1], FolderOf(kind), StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Key.Replace('\\', '/').TrimEnd('/'))
            .FirstOrDefault();
        if (standard is not null) return standard;

        var custom = ApplicationFolders.Where(entry => entry.Value == LayerOf(kind))
            .Select(entry => entry.Key.Replace('\\', '/').TrimEnd('/')).Take(2).ToArray();
        return custom.Length == 1 ? custom[0] : null;
    }

    /// <summary>
    /// Whether this layout can receive applications of a kind: it has their layer and, in folder mode, an application
    /// folder for them (see <see cref="ApplicationFolderOf"/>).
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns><see langword="true"/> if applications of <paramref name="kind"/> can be scaffolded.</returns>
    public bool Supports(ApplicationKind kind) =>
        Layers.Contains(LayerOf(kind)) && (Mode == LayeringMode.Prefix || ApplicationFolderOf(kind) is not null);

    /// <summary>
    /// The library project file of a layer, relative to the solution directory.
    /// </summary>
    /// <param name="layer">The layer name, e.g. <c>Core</c>.</param>
    /// <returns>
    /// <c>&lt;LibraryRoot&gt;/&lt;layer&gt;/&lt;layer&gt;.csproj</c> by folder, <c>&lt;Root&gt;/&lt;layer&gt;/&lt;layer&gt;.csproj</c> by prefix.
    /// </returns>
    public string LibraryProjectPath(string layer) => Mode == LayeringMode.Folder
        ? $"{Normalize(LibraryRoot ?? $"{Root}/{LibraryFolder}")}/{layer}/{layer}.csproj"
        : $"{Normalize(Root)}/{layer}/{layer}.csproj";

    /// <summary>
    /// The application's project name: its plain name in <see cref="LayeringMode.Folder"/>, otherwise prefixed with
    /// its layer and kind (e.g. <c>Web.Api.Orders</c>).
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The project name.</returns>
    public string ProjectName(ApplicationSpec app) => Mode == LayeringMode.Folder
        ? app.Name
        : $"{LayerOf(app.Kind)}.{PrefixOf(app.Kind)}.{app.Name}";

    /// <summary>
    /// The application's root namespace (and assembly name): its folder and name in <see cref="LayeringMode.Folder"/>
    /// (e.g. <c>Api.Orders</c>), otherwise its <see cref="ProjectName"/>.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The root namespace.</returns>
    public string RootNamespace(ApplicationSpec app) => Mode == LayeringMode.Folder
        ? $"{FolderOf(app.Kind)}.{app.Name}"
        : ProjectName(app);

    /// <summary>
    /// The project directory, relative to the solution directory.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The directory, with forward slashes.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown in folder mode when no application folder is configured for the kind (see <see cref="Supports"/>).
    /// </exception>
    public string ProjectDirectory(ApplicationSpec app) => Mode == LayeringMode.Folder
        ? $"{ApplicationFolderOf(app.Kind) ?? throw new InvalidOperationException(MissingApplicationFolder(app.Kind))}/{app.Name}"
        : $"{Normalize(Root)}/{ProjectName(app)}";

    /// <summary>
    /// The project file, relative to the solution directory.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The project file path, with forward slashes.</returns>
    public string ProjectPath(ApplicationSpec app) => $"{ProjectDirectory(app)}/{ProjectName(app)}.csproj";

    /// <summary>
    /// The library an application references: its own layer's, or null if the layout has no such layer.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns>The library project path, relative to the solution directory, or <see langword="null"/>.</returns>
    public string? LibraryOf(ApplicationKind kind) =>
        Supports(kind) ? LibraryProjectPath(LayerOf(kind)) : null;

    /// <summary>
    /// Why applications of a kind cannot be scaffolded into this folder layout: no application folder is mapped to
    /// their layer.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns>The message, naming the mapping to add.</returns>
    public string MissingApplicationFolder(ApplicationKind kind) =>
        $"The layering configuration maps no {FolderOf(kind)} folder to the {LayerOf(kind)} layer; add " +
        $"\"{Normalize(Root)}/{FolderOf(kind)}\": \"{LayerOf(kind)}\" to its applicationFolders.";

    private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');
}
