using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;

namespace Build;

/// <summary>
/// A same-layer project reference outside the package's name ancestors that the toolkit accepts on purpose.
/// </summary>
/// <param name="From">The referencing project (its package ID without <c>Anarchy.Toolkit.</c>).</param>
/// <param name="To">The referenced project.</param>
/// <param name="Reason">Why the packages share code across families.</param>
internal sealed record AllowedPackageReference(string From, string To, string Reason);

/// <summary>
/// The toolkit's package-name hierarchy, checked on top of the layering (see <see cref="LayeringTasks"/>), for this
/// repository only: adopters' solutions are never held to it.
/// </summary>
/// <remarks>
/// Only projects with a known layer are checked (<see cref="LayeringTasks.Testing"/> included, as its own layer), so
/// tests, samples and the build are left out. A reference to a lower layer is free, whatever its package family; a
/// reference to a higher layer is the layering check's to reject. A reference within the same layer must go to one of
/// the package's name ancestors (<c>Runtime.Web.MockServer.Ui</c> to <c>Runtime.Web.MockServer</c>, <c>Runtime.Web</c> or <c>Runtime</c>), unless the exact
/// pair is in <see cref="AllowedReferences"/>.
/// </remarks>
internal static class PackageHierarchy
{
    /// <summary>
    /// The same-layer references across package families that the toolkit accepts, each with its reason. An entry
    /// that no longer matches such a reference is stale and fails the check.
    /// </summary>
    public static readonly IReadOnlyList<AllowedPackageReference> AllowedReferences =
    [
        new("Core.Events", "Core.Messaging.Abstractions", "events are carried by the messaging contracts"),
        new("Core.Events", "Core.Telemetry", "event publishing and processing record telemetry"),
        new("Infrastructure.Messaging.Aws", "Infrastructure.CorrelationId", "AWS consumers open correlation scopes for inbound messages"),
        new("Infrastructure.Telemetry.Serilog", "Infrastructure.CorrelationId", "the opt-in Serilog enricher reads the correlation context"),
        new("Runtime.Web.MockServer.Ui", "Runtime.Web.Ui", "the mock server UI is hosted in the shared UI shell"),
    ];

    /// <summary>
    /// Whether <paramref name="ancestor"/> is a dotted prefix of <paramref name="package"/>, e.g. <c>Web</c> of
    /// <c>Web.MockServer.Ui</c>. A package is not its own ancestor.
    /// </summary>
    /// <param name="ancestor">The candidate ancestor.</param>
    /// <param name="package">The package.</param>
    /// <returns><see langword="true"/> if <paramref name="ancestor"/> is an ancestor of <paramref name="package"/>.</returns>
    public static bool IsAncestor(string ancestor, string package) =>
        package.StartsWith(ancestor + ".", StringComparison.Ordinal);

    /// <summary>
    /// Returns every package-hierarchy violation, one line each, in a stable order: same-layer references outside
    /// the package's name ancestors that are not allowed, then allowed references without a reason, then stale
    /// allowed references.
    /// </summary>
    /// <param name="projects">The projects, as returned by <see cref="LayeringTasks.Collect"/>.</param>
    /// <param name="layers">The layers, innermost first.</param>
    /// <param name="allowed">The accepted same-layer references across package families.</param>
    /// <returns>The violations; empty when the packages follow the hierarchy.</returns>
    public static IReadOnlyList<string> Check(
        IReadOnlyCollection<LayeringProject> projects,
        IReadOnlyList<string> layers,
        IReadOnlyCollection<AllowedPackageReference> allowed)
    {
        var violations = new List<string>();
        var layerByName = projects
            .Where(p => p.Layer == LayeringTasks.Testing || layers.Contains(p.Layer))
            .ToDictionary(p => p.Name, p => p.Layer);
        var allowedPairs = allowed.Select(a => (a.From, a.To)).ToHashSet();
        var used = new HashSet<(string From, string To)>();

        foreach (var project in projects.Where(p => layerByName.ContainsKey(p.Name)).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            foreach (var target in project.References.Distinct().Order(StringComparer.Ordinal))
            {
                if (!layerByName.TryGetValue(target, out var targetLayer) || targetLayer != project.Layer ||
                    IsAncestor(target, project.Name))
                {
                    continue;
                }

                if (allowedPairs.Contains((project.Name, target)))
                {
                    used.Add((project.Name, target));
                    continue;
                }

                violations.Add($"project {project.Name} ({project.Layer}) -> {target} ({targetLayer}) is outside its package-name " +
                               "ancestors: reference an ancestor or a lower layer, or allow the reference with a reason");
            }
        }

        foreach (var entry in allowed.Where(a => string.IsNullOrWhiteSpace(a.Reason)))
        {
            violations.Add($"allowed reference {entry.From} -> {entry.To} has no reason");
        }

        foreach (var entry in allowed.Where(a => !used.Contains((a.From, a.To))))
        {
            violations.Add($"allowed reference {entry.From} -> {entry.To} is stale: no same-layer reference outside the " +
                           "package-name ancestors matches it; remove it");
        }

        return violations;
    }
}
