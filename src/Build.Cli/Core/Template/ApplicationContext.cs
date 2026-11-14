using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Core.Template;

/// <summary>
/// What an application template needs to render one project; <c>LibraryReference</c> is the path of its layer's
/// library relative to the project directory, if any. <c>PackageVersion</c> is null when the solution manages package
/// versions centrally, so the reference carries none.
/// </summary>
public sealed record ApplicationContext(
    ApplicationSpec App,
    string ProjectName,
    string Namespace,
    string? PackageVersion,
    (int Http, int Https)? Ports,
    string? LibraryReference);
