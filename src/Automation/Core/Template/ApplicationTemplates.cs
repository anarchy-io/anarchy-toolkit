using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Core.Helpers.Ports;

namespace Anarchy.Toolkit.Automation.Core.Template;

/// <summary>
/// The starter files of each <see cref="ApplicationKind"/>, keyed by path relative to the project directory.
/// </summary>
public static class ApplicationTemplates
{
    /// <summary>
    /// The starter files of the context's application, keyed by path relative to the project directory.
    /// </summary>
    /// <param name="context">The application to render.</param>
    /// <returns>The file contents, keyed by path relative to the project directory.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the application kind is unknown.</exception>
    public static IReadOnlyDictionary<string, string> Render(ApplicationContext context)
    {
        var files = context.App.Kind switch {
            ApplicationKind.Api => ApiTemplate.Render(context),
            ApplicationKind.Ui => UiTemplate.Render(context),
            ApplicationKind.Job => JobTemplate.Render(context),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.App.Kind, null)
        };
        return context.SharedSettings is null
            ? files
            : files.Where(file => file.Key is not ("Composition/DependencyInjection/ModuleAppSettings.cs" or
                    "Infrastructure/Entity/Configuration/AppSettings.cs" or "appsettings.json"))
                .ToDictionary(file => file.Key, file => file.Value);
    }

    /// <summary>
    /// A library project of a layer, referencing the layer below it unless <paramref name="lowerLayerReference"/>
    /// (relative to the library's directory) is null.
    /// </summary>
    /// <param name="lowerLayerReference">The lower layer's project path, or <see langword="null"/> for the innermost layer.</param>
    /// <param name="rootNamespace">The root namespace and assembly name, or <see langword="null"/> for the project name.</param>
    /// <param name="packages">The package references, each versioned unless its version is null (central package management).</param>
    /// <returns>The <c>.csproj</c> content.</returns>
    public static string Library(string? lowerLayerReference, string? rootNamespace = null,
        IEnumerable<(string Id, string? Version)>? packages = null) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
        {{(rootNamespace is null ? "" : $"    <RootNamespace>{rootNamespace}</RootNamespace>\n    <AssemblyName>{rootNamespace}</AssemblyName>\n")}}  </PropertyGroup>
        {{Packages(packages)}}{{References(lowerLayerReference)}}</Project>

        """;

    private static string Packages(IEnumerable<(string Id, string? Version)>? packages) =>
        packages?.ToArray() is { Length: > 0 } list
            ? "  <ItemGroup>\n" + string.Concat(list.Select(package => $"    {PackageReference(package.Id, package.Version)}\n")) + "  </ItemGroup>\n"
            : "";

    /// <summary>
    /// A non-packable application project on <paramref name="sdk"/>, referencing its toolkit packages and layer library.
    /// </summary>
    internal static string Project(string sdk, ApplicationContext context, bool exe) => $$"""
        <Project Sdk="{{sdk}}">
          <PropertyGroup>
        {{(exe ? $"    <OutputType>Exe</OutputType>\n    <StartupObject>{context.Namespace}.EntryPointLocal</StartupObject>\n" : "")}}    <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <RootNamespace>{{context.Namespace}}</RootNamespace>
            <AssemblyName>{{context.Namespace}}</AssemblyName>
            <!-- An application, not a library: keep it out of NuGet packing. -->
            <IsPackable>false</IsPackable>
          </PropertyGroup>
          <ItemGroup>
            {{PackageReference(context.App.PackageId, context.PackageVersion)}}{{(context.App.HasSwagger ? "\n    " + PackageReference(ApplicationSpec.SwaggerPackageId, context.SwaggerPackageVersion) : "")}}
          </ItemGroup>
        {{References(context.LibraryReference)}}</Project>

        """;

    /// <summary>
    /// A <c>PackageReference</c>, versioned unless <paramref name="version"/> is null (central package management).
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <param name="version">The version, or <see langword="null"/> to omit the <c>Version</c> attribute.</param>
    /// <returns>The <c>PackageReference</c> element.</returns>
    public static string PackageReference(string packageId, string? version) => version is null
        ? $"<PackageReference Include=\"{packageId}\" />"
        : $"<PackageReference Include=\"{packageId}\" Version=\"{version}\" />";

    private static string References(string? reference) => reference is null
        ? ""
        : $"""
            <ItemGroup>
              <ProjectReference Include="{reference.Replace('/', '\\')}" />
            </ItemGroup>

          """;

    /// <summary>
    /// The application's local ports, or the first pair when none were assigned.
    /// </summary>
    internal static (int Http, int Https) PortsOf(ApplicationContext context) => context.Ports ?? (LocalPorts.First, LocalPorts.First + 1);

    /// <summary>
    /// A PascalCase name in kebab case (e.g. <c>OrderHistory</c> becomes <c>order-history</c>).
    /// </summary>
    internal static string Kebab(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
