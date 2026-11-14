namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// Everything needed to bootstrap a solution into an empty directory.
/// </summary>
public sealed record SolutionPlan(string Name, string RootDirectory, SolutionLayout Layout, IReadOnlyList<ApplicationSpec> Applications)
{
    /// <summary>
    /// A plan for the baseline solution, in the default layout and without applications.
    /// </summary>
    public SolutionPlan(string name, string rootDirectory) : this(name, rootDirectory, SolutionLayout.Default, []) { }

    /// <summary>
    /// The local HTTP and HTTPS ports of each web host, in the order the applications were added.
    /// </summary>
    public IReadOnlyDictionary<ApplicationSpec, (int Http, int Https)> Ports => LocalPorts.Assign(Applications, []);
}
