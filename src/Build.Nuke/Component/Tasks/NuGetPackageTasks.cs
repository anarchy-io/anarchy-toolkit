using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    /// <summary>Validates and orders a batch of NuGet packages before any are pushed.</summary>
    public static class NuGetPackageTasks
    {
        internal static readonly string InvocationId = Guid.NewGuid().ToString("N");

        /// <summary>Finds primary packages, excluding legacy symbol packages stored next to them.</summary>
        public static IEnumerable<string> FindPackages(string directory) =>
            Directory.EnumerateFiles(directory, "*.nupkg", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Topologically sorts actual package dependencies across all target frameworks. Dependencies outside the batch
        /// are assumed to already be available on the feed. Cycles, duplicate IDs and incompatible versions fail
        /// before publishing anything.
        /// </summary>
        public static IReadOnlyList<string> OrderForPublish(IEnumerable<string> packagePaths)
        {
            var packages = new Dictionary<string, (string Path, PackageIdentity Identity, PackageDependency[] Dependencies)>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in packagePaths.Order(StringComparer.Ordinal))
            {
                using var reader = new PackageArchiveReader(path);
                var identity = reader.GetIdentity();
                var dependencies = reader.GetPackageDependencies().SelectMany(group => group.Packages).ToArray();
                if (!packages.TryAdd(identity.Id, (path, identity, dependencies)))
                    throw new InvalidOperationException($"Multiple packages with ID '{identity.Id}' were produced.");
            }

            var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            void Visit(string id)
            {
                if (state.TryGetValue(id, out var current))
                {
                    if (current == 1) throw new InvalidOperationException($"Package dependency cycle involving '{id}'.");
                    return;
                }

                state[id] = 1;
                var package = packages[id];
                foreach (var dependency in package.Dependencies.OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase))
                {
                    if (!packages.TryGetValue(dependency.Id, out var target)) continue;
                    if (dependency.VersionRange is not null && !dependency.VersionRange.Satisfies(target.Identity.Version))
                        throw new InvalidOperationException($"{package.Identity} requires {dependency.Id} {dependency.VersionRange}, but the batch contains {target.Identity.Version}.");
                    Visit(dependency.Id);
                }

                state[id] = 2;
                result.Add(package.Path);
            }

            foreach (var id in packages.Keys.Order(StringComparer.OrdinalIgnoreCase)) Visit(id);
            return result;
        }
    }
}
