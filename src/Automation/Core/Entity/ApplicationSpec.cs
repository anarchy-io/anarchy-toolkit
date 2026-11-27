namespace Anarchy.Toolkit.Automation.Core.Entity;

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
/// disables it. Ignored for other kinds and when <see cref="Swagger"/> is off.
/// </param>
public sealed record ApplicationSpec(ApplicationKind Kind, string Name, bool SwaggerTheme = true)
{
    /// <summary>The package an API references for its OpenAPI document and Swagger UI.</summary>
    public const string SwaggerPackageId = "Anarchy.Toolkit.Runtime.Web.Swagger";

    /// <summary>
    /// Whether an API includes Swagger (<see cref="SwaggerPackageId"/>, enabled in the generated startup).
    /// Ignored for other kinds.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool Swagger { get; init; } = true;

    /// <summary>Whether the application is an API that includes Swagger.</summary>
    public bool HasSwagger => Kind == ApplicationKind.Api && Swagger;

    /// <summary>Whether to generate a container host for a web application.</summary>
    public bool Container { get; init; }

    /// <summary>Whether to generate an API Gateway Lambda handler for an API.</summary>
    public bool ApiLambda { get; init; }

    /// <summary>Whether to generate a scheduled Lambda handler for a job.</summary>
    public bool EventLambda { get; init; }

    /// <summary>How a job runs locally; defaults to a long-running worker.</summary>
    public JobExecutionMode JobMode { get; init; } = JobExecutionMode.Worker;

    /// <summary>
    /// Whether the application listens on local HTTP ports.
    /// </summary>
    /// <value><see langword="true"/> for <see cref="ApplicationKind.Api"/> and <see cref="ApplicationKind.Ui"/>.</value>
    public bool IsWebHost => Kind is ApplicationKind.Api or ApplicationKind.Ui;

    /// <summary>
    /// The toolkit package the application is built on.
    /// </summary>
    /// <value>
    /// <c>Anarchy.Toolkit.Runtime.Web</c> for an API (<c>Anarchy.Toolkit.Runtime.Web.Aws</c> with <see cref="ApiLambda"/>),
    /// <c>Anarchy.Toolkit.Runtime.Web.Ui</c> for a UI, <c>Anarchy.Toolkit.Runtime</c> for a job
    /// (<c>Anarchy.Toolkit.Runtime.Aws</c> with <see cref="EventLambda"/>). Applications without a Lambda entry point
    /// reference no AWS package.
    /// </value>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <see cref="Kind"/> is not a known value.</exception>
    public string PackageId => Kind switch {
        ApplicationKind.Api when ApiLambda => "Anarchy.Toolkit.Runtime.Web.Aws",
        ApplicationKind.Api => "Anarchy.Toolkit.Runtime.Web",
        ApplicationKind.Ui => "Anarchy.Toolkit.Runtime.Web.Ui",
        ApplicationKind.Job when EventLambda => "Anarchy.Toolkit.Runtime.Aws",
        ApplicationKind.Job => "Anarchy.Toolkit.Runtime",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null)
    };

    /// <summary>
    /// Every toolkit package the application references: <see cref="PackageId"/>, then <see cref="SwaggerPackageId"/>
    /// for an API with <see cref="Swagger"/>.
    /// </summary>
    public IReadOnlyList<string> PackageIds => HasSwagger ? [PackageId, SwaggerPackageId] : [PackageId];
}
