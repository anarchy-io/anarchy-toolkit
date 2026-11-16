using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Core.Template;

/// <summary>
/// What an application template needs to render one project; <c>LibraryReference</c> is the path of its layer's
/// library relative to the project directory, if any. <c>PackageVersion</c> is null when the solution manages package
/// versions centrally, so the reference carries none.
/// </summary>
/// <remarks>Built by the scaffolder from a <see cref="SolutionLayout"/> and passed to <see cref="ApplicationTemplates.Render"/>.</remarks>
/// <param name="App">The application being rendered.</param>
/// <param name="ProjectName">The project name (see <see cref="SolutionLayout.ProjectName"/>).</param>
/// <param name="Namespace">The root namespace and assembly name (see <see cref="SolutionLayout.RootNamespace"/>).</param>
/// <param name="PackageVersion">
/// The version of <see cref="ApplicationSpec.PackageId"/>, or <see langword="null"/> under central package management.
/// </param>
/// <param name="Ports">The local HTTP and HTTPS ports of a web host, or <see langword="null"/> when none were assigned.</param>
/// <param name="LibraryReference">
/// The layer library's project path relative to the project directory, or <see langword="null"/> if there is none.
/// </param>
public sealed record ApplicationContext(
    ApplicationSpec App,
    string ProjectName,
    string Namespace,
    string? PackageVersion,
    (int Http, int Https)? Ports,
    string? LibraryReference);
