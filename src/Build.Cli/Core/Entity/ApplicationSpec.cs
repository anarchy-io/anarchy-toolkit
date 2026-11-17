namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// An application to add to a solution; where it goes depends on the <see cref="SolutionLayout"/>.
/// </summary>
/// <remarks>
/// Instances are listed in <see cref="SolutionPlan.Applications"/> and resolved to project names, folders and
/// namespaces by <see cref="SolutionLayout"/>. Being a record, two specs with the same kind and name are equal, which
/// makes them usable as dictionary keys (e.g. in <c>LocalPorts.Assign</c>).
/// </remarks>
/// <param name="Kind">The kind of application, which selects its template, layer and toolkit package.</param>
/// <param name="Name">The application name, e.g. <c>Orders</c>.</param>
/// <param name="SwaggerTheme">
/// Whether an API keeps the toolkit's default Swagger UI theme; when <see langword="false"/>, the generated startup
/// disables it. Ignored for other kinds.
/// </param>
public sealed record ApplicationSpec(ApplicationKind Kind, string Name, bool SwaggerTheme = true)
{
    /// <summary>
    /// Whether the application listens on local HTTP ports.
    /// </summary>
    /// <value><see langword="true"/> for <see cref="ApplicationKind.Api"/> and <see cref="ApplicationKind.Ui"/>.</value>
    public bool IsWebHost => Kind is ApplicationKind.Api or ApplicationKind.Ui;

    /// <summary>
    /// The toolkit package the application is built on.
    /// </summary>
    /// <value>
    /// <c>Anarchy.Toolkit.Web</c> for an API, <c>Anarchy.Toolkit.Web.Ui</c> for a UI, <c>Anarchy.Toolkit.Hosting</c>
    /// for a job.
    /// </value>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <see cref="Kind"/> is not a known value.</exception>
    public string PackageId => Kind switch {
        ApplicationKind.Api => "Anarchy.Toolkit.Web",
        ApplicationKind.Ui => "Anarchy.Toolkit.Web.Ui",
        ApplicationKind.Job => "Anarchy.Toolkit.Hosting",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null)
    };
}
