using Anarchy.Toolkit.Automation.Core.Entity;

namespace Anarchy.Toolkit.Automation.Core.Template;

/// <summary>
/// The files of a solution's shared settings: <c>AppSettings</c> in the Infrastructure library, and
/// <c>ModuleAppSettings</c> with the <c>appsettings*.json</c> files in the Bootstrap library, which the root
/// <c>Directory.Build.props</c> links into every other project's output.
/// </summary>
public static class SharedAppSettingsTemplates
{
    /// <summary>
    /// The path of the shared <c>AppSettings</c> class, relative to the Infrastructure library's directory.
    /// </summary>
    public const string AppSettingsPath = "Entity/Configuration/AppSettings.cs";

    /// <summary>
    /// The package the shared settings libraries reference.
    /// </summary>
    public const string PackageId = "Anarchy.Toolkit.Bootstrap";

    /// <summary>
    /// The Infrastructure library's files, keyed by path relative to its directory.
    /// </summary>
    /// <param name="shared">The libraries' root namespaces.</param>
    /// <returns>The file contents.</returns>
    public static IReadOnlyDictionary<string, string> Infrastructure(SharedAppSettings shared) => new Dictionary<string, string> {
        [AppSettingsPath] = $$"""
            using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
            using System.ComponentModel.DataAnnotations;

            namespace {{shared.InfrastructureNamespace}}.Entity.Configuration;

            /// <summary>
            /// The settings every application of the solution binds from the shared <c>appsettings*.json</c> files of the
            /// Bootstrap library. Implement further capability interfaces (e.g. <c>IAuthServiceAppSettings</c>) as needed.
            /// </summary>
            public sealed class AppSettings : IAppSettings
            {
                /// <summary>
                /// The OAuth service UI users sign in against with their client credentials.
                /// </summary>
                [Required] public Uri AuthServiceBaseUrl { get; init; } = new("https://auth.example.local/");

                /// <summary>
                /// The scope a UI requests at login; empty grants every scope allowed for the client.
                /// </summary>
                public string? Scope { get; init; }
            }

            """
    };

    /// <summary>
    /// The Bootstrap library's files, keyed by path relative to its directory.
    /// </summary>
    /// <param name="shared">The libraries' root namespaces.</param>
    /// <returns>The file contents.</returns>
    public static IReadOnlyDictionary<string, string> Bootstrap(SharedAppSettings shared) => new Dictionary<string, string> {
        ["DependencyInjection/ModuleAppSettings.cs"] = $$"""
            using {{shared.InfrastructureNamespace}}.Entity.Configuration;
            using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration.Entity;

            namespace {{shared.BootstrapNamespace}}.DependencyInjection;

            /// <summary>
            /// Binds the shared <see cref="AppSettings"/> and registers it as itself and as <c>IAppSettings</c>, which the
            /// toolkit's components resolve at runtime. Override <c>RegisterAdditionalTypesForSettings</c> to expose
            /// further capability interfaces.
            /// </summary>
            public sealed class ModuleAppSettings : ModuleAppSettingsBase<AppSettings>;

            """,
        ["appsettings.json"] = """
            {
              "AuthServiceBaseUrl": "https://auth.example.local/"
            }

            """,
        ["appsettings.Development.json"] = """
            {
            }

            """
    };

    /// <summary>
    /// The root <c>Directory.Build.props</c> item group that links the Bootstrap library's <c>appsettings*.json</c>
    /// into the output of every project outside the libraries and the build project.
    /// </summary>
    /// <param name="layout">The solution's layout.</param>
    /// <returns>The <c>ItemGroup</c> element, indented for the root <c>Project</c>.</returns>
    public static string DirectoryBuildProps(SolutionLayout layout)
    {
        var bootstrap = layout.LibraryProjectPath(SolutionLayout.BootstrapLayer);
        var directory = bootstrap[..bootstrap.LastIndexOf('/')].Replace('/', '\\');
        var libraries = layout.Mode == LayeringMode.Folder
            ? Pattern(layout.LibraryRoot ?? $"{layout.Root}/{SolutionLayout.LibraryFolder}") + @"([\\/]|$)"
            : Pattern(layout.Root) + @"[\\/]" + SolutionLayout.LibraryFolder + @"\.";
        return $$"""
              <!-- The shared settings of {{directory}}, copied next to every application and test assembly. -->
              <ItemGroup Condition="!$([System.Text.RegularExpressions.Regex]::IsMatch($([MSBuild]::MakeRelative('$(MSBuildThisFileDirectory)', '$(MSBuildProjectDirectory)')), '^({{libraries}}|build([\\/]|$))'))">
                <Content Include="$(MSBuildThisFileDirectory){{directory}}\appsettings*.json"
                         Link="%(Filename)%(Extension)"
                         CopyToOutputDirectory="PreserveNewest" />
              </ItemGroup>

            """;
    }

    private static string Pattern(string path) =>
        string.Join(@"[\\/]", path.Replace('\\', '/').Trim('/').Split('/').Select(System.Text.RegularExpressions.Regex.Escape));
}
