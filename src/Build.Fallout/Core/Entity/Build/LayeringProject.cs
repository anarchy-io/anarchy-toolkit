namespace Anarchy.Toolkit.Build.Fallout.Core.Entity.Build
{
    /// <summary>A project as checked by the <c>CheckLayering</c> target.</summary>
    /// <param name="Name">The project name, e.g. <c>Infrastructure.Http</c>.</param>
    /// <param name="Layer">
    /// Its layer, <c>Testing</c> for a test kit, or any other value for a project that should be in a layer but is not
    /// (reported as a violation).
    /// </param>
    /// <param name="RootNamespace">The namespace the project owns, e.g. <c>Anarchy.Toolkit.Infrastructure.Http</c>.</param>
    /// <param name="References">Names of the projects it references.</param>
    /// <param name="Sources">Its <c>.cs</c> files (<c>bin</c>/<c>obj</c> excluded): path relative to the project, and content.</param>
    /// <param name="RootFolders">Names of the folders at the project's root.</param>
    public sealed record LayeringProject(
        string Name,
        string Layer,
        string RootNamespace,
        IReadOnlyCollection<string> References,
        IReadOnlyDictionary<string, string> Sources,
        IReadOnlyCollection<string> RootFolders);
}
