using LocalPorts = Anarchy.Toolkit.Core.Helper.LocalPorts;
namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// Everything needed to bootstrap a solution into an empty directory.
/// </summary>
/// <remarks>Consumed by <see cref="Anarchy.Toolkit.Build.Cli.Core.Service.SolutionBootstrapper"/>.</remarks>
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
    /// Whether to install the toolkit's agent skills into <c>.agents/skills</c>; off by default.
    /// </summary>
    public bool AgentSkills { get; init; }

    /// <summary>
    /// The local HTTP and HTTPS ports of each web host, in the order the applications were added.
    /// </summary>
    public IReadOnlyDictionary<ApplicationSpec, (int Http, int Https)> Ports => LocalPorts.Assign(Applications.Where(app => app.IsWebHost), []);
}
