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
    /// The layer applications (APIs, jobs and UIs) belong to. It has no library project: applications reference
    /// the library of the layer below it.
    /// </summary>
    public const string AppsLayer = "Apps";

    /// <summary>
    /// The layer of the runtime (hosting, workers and web) libraries, right below <see cref="AppsLayer"/>.
    /// </summary>
    public const string RuntimeLayer = "Runtime";

    /// <summary>
    /// The layer whose library holds a monorepo's shared <c>AppSettings</c>.
    /// </summary>
    public const string InfrastructureLayer = "Infrastructure";

    /// <summary>
    /// The layer whose library holds a monorepo's shared <c>ModuleAppSettings</c> and <c>appsettings*.json</c>.
    /// </summary>
    public const string BootstrapLayer = "Bootstrap";

    /// <summary>
    /// The layers a new solution starts with, innermost first: one library project is created for each but
    /// <see cref="AppsLayer"/>.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultLayers =
        ["Core", InfrastructureLayer, BootstrapLayer, RuntimeLayer, AppsLayer];

    /// <summary>
    /// Every <see cref="ApplicationKind"/>, in the order their application folders are configured.
    /// </summary>
    public static IReadOnlyList<ApplicationKind> Kinds { get; } = [ApplicationKind.Api, ApplicationKind.Job, ApplicationKind.Ui];

    /// <summary>
    /// The layout of a new solution: <see cref="LayeringMode.Folder"/> over the <see cref="DefaultLayers"/>.
    /// </summary>
    public static SolutionLayout Default { get; } = new(LayeringMode.Folder, DefaultLayers);

    /// <summary>
    /// The layers that get a library project: every layer but <see cref="AppsLayer"/>.
    /// </summary>
    public IReadOnlyList<string> LibraryLayers => Layers.Where(layer => layer != AppsLayer).ToArray();

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
    /// (e.g. <c>src/Apps/Apis</c> → <c>Apps</c>), checked before <see cref="LibraryRoot"/>. Defaults to one folder per
    /// <see cref="Kinds">application kind</see> whose layer is in <see cref="Layers"/> in folder mode, and is empty in
    /// prefix mode.
    /// </summary>
    public IReadOnlyDictionary<string, string> ApplicationFolders { get; init; } = Mode == LayeringMode.Folder
        ? Kinds.Where(kind => Layers.Contains(LayerOf(kind)))
            .ToDictionary(kind => $"{DefaultRoot}/{AppsLayer}/{FolderOf(kind)}", LayerOf)
        : new Dictionary<string, string>();

    /// <summary>
    /// The layer an application belongs to.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns><see cref="AppsLayer"/>, whatever the kind.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="kind"/> is not a known value.</exception>
    public static string LayerOf(ApplicationKind kind) =>
        Enum.IsDefined(kind) ? AppsLayer : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);

    /// <summary>
    /// The name of the application folder that holds the applications of a kind in <see cref="LayeringMode.Folder"/>.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns><c>Apis</c>, <c>Jobs</c> or <c>Ui</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="kind"/> is not a known value.</exception>
    public static string FolderOf(ApplicationKind kind) => kind switch {
        ApplicationKind.Api => "Apis",
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
    /// the one named <see cref="FolderOf"/> (e.g. <c>src/Apps/Apis</c>) and mapped to <see cref="LayerOf"/>.
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
    /// <c>&lt;LibraryRoot&gt;/&lt;layer&gt;/&lt;layer&gt;.csproj</c> by folder,
    /// <c>&lt;Root&gt;/Library.&lt;layer&gt;/Library.&lt;layer&gt;.csproj</c> by prefix.
    /// </returns>
    public string LibraryProjectPath(string layer) => Mode == LayeringMode.Folder
        ? $"{Normalize(LibraryRoot ?? $"{Root}/{LibraryFolder}")}/{layer}/{layer}.csproj"
        : $"{Normalize(Root)}/{LibraryFolder}.{layer}/{LibraryFolder}.{layer}.csproj";

    /// <summary>
    /// The application's project name: its plain name in <see cref="LayeringMode.Folder"/>, otherwise prefixed with
    /// its layer and kind (e.g. <c>Apps.Api.Orders</c>).
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The project name.</returns>
    public string ProjectName(ApplicationSpec app) => Mode == LayeringMode.Folder
        ? app.Name
        : $"{LayerOf(app.Kind)}.{PrefixOf(app.Kind)}.{app.Name}";

    /// <summary>
    /// The application's root namespace (and assembly name): its directory under <see cref="Root"/> in
    /// <see cref="LayeringMode.Folder"/> (e.g. <c>Apps.Apis.Orders</c>), otherwise its <see cref="ProjectName"/>.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The root namespace.</returns>
    public string RootNamespace(ApplicationSpec app)
    {
        if (Mode != LayeringMode.Folder) return ProjectName(app);
        var directory = ProjectDirectory(app);
        var root = Normalize(Root);
        if (root != "." && directory.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)) directory = directory[(root.Length + 1)..];
        return directory.Replace('/', '.');
    }

    /// <summary>
    /// The application's root namespace prefixed with the solution name, e.g. <c>Shop.Apps.Apis.Orders</c>.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="solutionName">The solution name.</param>
    /// <returns>The root namespace.</returns>
    public string RootNamespace(ApplicationSpec app, string solutionName) => $"{solutionName}.{RootNamespace(app)}";

    /// <summary>
    /// The root namespace of a layer's library prefixed with the solution name: its directory under <see cref="Root"/>,
    /// e.g. <c>Shop.Library.Infrastructure</c> (<c>src/Library/Infrastructure</c> by folder, <c>src/Library.Infrastructure</c>
    /// by prefix).
    /// </summary>
    /// <param name="layer">The layer.</param>
    /// <param name="solutionName">The solution name.</param>
    /// <returns>The root namespace.</returns>
    public string LibraryNamespace(string layer, string solutionName)
    {
        var path = LibraryProjectPath(layer);
        return Namespace(solutionName, UnderRoot(path[..path.LastIndexOf('/')]));
    }

    /// <summary>
    /// Whether the layout can share a single <c>AppSettings</c> across its applications: it has the
    /// <see cref="InfrastructureLayer"/> and <see cref="BootstrapLayer"/> and keeps its libraries apart from its
    /// applications (not a single-app layout, whose libraries sit directly under <see cref="Root"/>).
    /// </summary>
    public bool SupportsSharedAppSettings =>
        Layers.Contains(InfrastructureLayer) && Layers.Contains(BootstrapLayer) &&
        (Mode == LayeringMode.Prefix || !string.Equals(Normalize(LibraryRoot ?? Root), Normalize(Root), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A namespace made of the solution name and a solution-relative path, each segment starting in upper case,
    /// e.g. <c>Shop.Tests.Unit</c> for <c>tests/Unit</c>.
    /// </summary>
    /// <param name="solutionName">The solution name.</param>
    /// <param name="relativePath">The path, with forward or back slashes.</param>
    /// <returns>The namespace.</returns>
    public static string Namespace(string solutionName, string relativePath) =>
        string.Join('.', relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => segment != ".")
            .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..])
            .Prepend(solutionName));

    private string UnderRoot(string path)
    {
        var root = Normalize(Root);
        return root != "." && path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) ? path[(root.Length + 1)..] : path;
    }

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
    /// The library an application references: the one of the layer right below its own, or null if there is none.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns>The library project path, relative to the solution directory, or <see langword="null"/>.</returns>
    public string? LibraryOf(ApplicationKind kind)
    {
        if (!Supports(kind)) return null;
        var index = Layers.ToList().IndexOf(LayerOf(kind));
        return index > 0 ? LibraryProjectPath(Layers[index - 1]) : null;
    }

    /// <summary>
    /// Why applications of a kind cannot be scaffolded into this folder layout: no application folder is mapped to
    /// their layer.
    /// </summary>
    /// <param name="kind">The application kind.</param>
    /// <returns>The message, naming the mapping to add.</returns>
    public string MissingApplicationFolder(ApplicationKind kind) =>
        $"The layering configuration maps no {FolderOf(kind)} folder to the {LayerOf(kind)} layer; add " +
        $"\"{Normalize(Root)}/{AppsLayer}/{FolderOf(kind)}\": \"{LayerOf(kind)}\" to its applicationFolders.";

    private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');
}
