namespace Anarchy.Toolkit.Build.Fallout.Core.Entity.Build
{
    /// <summary>
    /// The project graph of a solution, grouped by layer, as rendered by the <c>GenerateDependencyMap</c> target.
    /// </summary>
    /// <param name="Title">Shown as the page heading, usually the solution name.</param>
    /// <param name="Layers">The layered chain, innermost first (e.g. <c>Core</c> … <c>Web</c>).</param>
    /// <param name="Projects">Every mapped project, in any order.</param>
    /// <param name="References">Project references between mapped projects, in any order.</param>
    public sealed record DependencyMap(
        string Title,
        IReadOnlyList<string> Layers,
        IReadOnlyCollection<DependencyMapProject> Projects,
        IReadOnlyCollection<DependencyMapReference> References)
    {
        /// <summary>
        /// The groups in display order: the <see cref="Layers"/> (inner to outer) followed by the groups outside the
        /// chain (test kits, tests, samples, ...), alphabetically. Layers without projects are kept.
        /// </summary>
        public IReadOnlyList<string> Groups =>
            Layers
                .Concat(Projects.Select(p => p.Group).Where(g => !IsLayer(g)).Distinct().Order(StringComparer.Ordinal))
                .ToArray();

        public bool IsLayer(string group) => Layers.Contains(group);

        /// <summary>
        /// Whether a reference breaks the layering: it points to a higher layer, or from a layer to a group outside
        /// the chain (production code depending on a test kit, a sample, ...).
        /// </summary>
        public bool IsViolation(DependencyMapReference reference)
        {
            var from = GroupOf(reference.From);
            var to = GroupOf(reference.To);

            if (from is null || to is null || !IsLayer(from))
            {
                return false;
            }

            return !IsLayer(to) || Rank(to) > Rank(from);
        }

        private string? GroupOf(string project) => Projects.FirstOrDefault(p => p.Name == project)?.Group;

        private int Rank(string layer) => Layers.ToList().IndexOf(layer);
    }

    /// <param name="Name">The project name.</param>
    /// <param name="Group">Its layer, or a group outside the chain (e.g. <c>Tests</c>).</param>
    /// <param name="SourceFiles">Number of <c>.cs</c> files, <c>bin</c>/<c>obj</c> excluded.</param>
    /// <param name="SourceLines">Number of non-blank lines in those files.</param>
    public sealed record DependencyMapProject(string Name, string Group, int SourceFiles, int SourceLines);

    /// <param name="From">The referencing project.</param>
    /// <param name="To">The referenced project.</param>
    public sealed record DependencyMapReference(string From, string To);
}
