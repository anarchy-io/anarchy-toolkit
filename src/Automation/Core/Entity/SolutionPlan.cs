using Anarchy.Toolkit.Core.Helpers.Ports;
namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>
/// Everything needed to bootstrap a solution into an empty directory.
/// </summary>
/// <remarks>Consumed by <see cref="Anarchy.Toolkit.Automation.Core.Service.SolutionBootstrapper"/>.</remarks>
/// <param name="Name">The solution name.</param>
/// <param name="RootDirectory">The (empty) directory the solution is created in.</param>
/// <param name="Layout">Where projects go and which layers they are checked against.</param>
/// <param name="Applications">The applications to add, in order.</param>
public sealed record SolutionPlan(string Name, string RootDirectory, SolutionLayout Layout, IReadOnlyList<ApplicationSpec> Applications)
{
    /// <summary>
    /// A plan for the baseline solution, in the default layout and without applications.
    /// </summary>
    /// <param name="name">The solution name.</param>
    /// <param name="rootDirectory">The (empty) directory the solution is created in.</param>
    public SolutionPlan(string name, string rootDirectory) : this(name, rootDirectory, SolutionLayout.Default, []) { }

    /// <summary>
    /// The test kinds the build recognizes, each created as <c>tests/&lt;Kind&gt;/&lt;Kind&gt;.csproj</c>.
    /// </summary>
    public static IReadOnlyList<string> AllTestKinds { get; } = ["Unit", "Integration", "EndToEnd"];

    /// <summary>
    /// The test projects to create (see <see cref="AllTestKinds"/>); none by default.
    /// </summary>
    public IReadOnlyList<string> TestKinds { get; init; } = [];

    /// <summary>
    /// The solution-relative project file of a test kind.
    /// </summary>
    /// <param name="kind">The test kind, e.g. <c>Unit</c>.</param>
    /// <returns><c>tests/&lt;kind&gt;/&lt;kind&gt;.csproj</c>.</returns>
    public static string TestProjectPath(string kind) => $"tests/{kind}/{kind}.csproj";

    /// <summary>
    /// The default name of the shared test project every test project references.
    /// </summary>
    public const string DefaultCommonTestProject = "Common";

    /// <summary>
    /// The name of the shared, non-executable test project created with the first test project
    /// (<c>tests/&lt;name&gt;/&lt;name&gt;.csproj</c>); <see cref="DefaultCommonTestProject"/> by default.
    /// </summary>
    public string CommonTestProject { get; init; } = DefaultCommonTestProject;

    /// <summary>
    /// Whether to create a mock server host in <c>tests/MockServer</c>; off by default.
    /// </summary>
    public bool MockServer { get; init; }

    /// <summary>
    /// Whether the applications share a single <c>AppSettings</c> (Infrastructure library) and <c>ModuleAppSettings</c>
    /// with its <c>appsettings*.json</c> (Bootstrap library) instead of each having its own; requires a layout that
    /// <see cref="SolutionLayout.SupportsSharedAppSettings">supports it</see>. Off by default.
    /// </summary>
    public bool SharedAppSettings { get; init; }

    /// <summary>
    /// Whether to install the toolkit's agent skills into <c>.agents/skills</c>; off by default.
    /// </summary>
    public bool AgentSkills { get; init; }

    /// <summary>Whether to save this plan's deliberately selected layering preferences after successful generation.</summary>
    public bool SavePreferences { get; init; }

    /// <summary>
    /// The local HTTP and HTTPS ports of each web host, in the order the applications were added.
    /// </summary>
    public IReadOnlyDictionary<ApplicationSpec, (int Http, int Https)> Ports => LocalPorts.Assign(Applications.Where(app => app.IsWebHost), []);
}
