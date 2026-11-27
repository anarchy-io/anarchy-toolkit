using Anarchy.Toolkit.Automation.Core.Entity;

namespace Anarchy.Toolkit.Automation.Core.Template;

/// <summary>
/// What an application template needs to render one project; <c>LibraryReference</c> is the path of its layer's
/// library relative to the project directory, if any. <c>PackageVersion</c> is null when the solution manages package
/// versions centrally, so the reference carries none.
/// </summary>
/// <remarks>Built by the scaffolder from a <see cref="SolutionLayout"/> and passed to <see cref="ApplicationTemplates.Render"/>.</remarks>
/// <param name="App">The application being rendered.</param>
/// <param name="ProjectName">The project name (see <see cref="SolutionLayout.ProjectName"/>).</param>
/// <param name="Namespace">The root namespace and assembly name (see <see cref="SolutionLayout.RootNamespace(ApplicationSpec, string)"/>).</param>
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
    string? LibraryReference)
{
    /// <summary>
    /// The version of <see cref="ApplicationSpec.SwaggerPackageId"/> for an API with Swagger, or <see langword="null"/>
    /// under central package management.
    /// </summary>
    public string? SwaggerPackageVersion { get; init; }

    /// <summary>The effective layering mode; folder mode uses project-specific startup names.</summary>
    public LayeringMode LayeringMode { get; init; } = LayeringMode.Prefix;

    /// <summary>
    /// The root namespaces of the Infrastructure and Bootstrap libraries holding the solution's shared
    /// <c>AppSettings</c> and <c>ModuleAppSettings</c>, or <see langword="null"/> when the application has its own.
    /// </summary>
    public SharedAppSettings? SharedSettings { get; init; }

    /// <summary>The namespace of the <c>AppSettings</c> class the application uses.</summary>
    public string SettingsNamespace => SharedSettings is { } shared
        ? $"{shared.InfrastructureNamespace}.Entity.Configuration"
        : $"{Namespace}.Infrastructure.Entity.Configuration";

    /// <summary>The namespace of the <c>ModuleAppSettings</c> class the application uses.</summary>
    public string ModuleNamespace => SharedSettings is { } shared
        ? $"{shared.BootstrapNamespace}.DependencyInjection"
        : $"{Namespace}.Composition.DependencyInjection";

    /// <summary>The startup class and filename for this application.</summary>
    public string StartupName => LayeringMode == LayeringMode.Folder
        ? App.Name + "Startup"
        : App.Kind == ApplicationKind.Job ? "JobStartup" : "Startup";
}

/// <summary>
/// The root namespaces of the libraries holding a solution's shared settings.
/// </summary>
/// <param name="InfrastructureNamespace">The Infrastructure library's root namespace, holding <c>AppSettings</c>.</param>
/// <param name="BootstrapNamespace">The Bootstrap library's root namespace, holding <c>ModuleAppSettings</c>.</param>
public sealed record SharedAppSettings(string InfrastructureNamespace, string BootstrapNamespace);
