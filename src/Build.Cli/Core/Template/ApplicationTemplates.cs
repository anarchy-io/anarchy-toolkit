using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Core.Template;

/// <summary>
/// The starter files of each <see cref="ApplicationKind"/>, keyed by path relative to the project directory.
/// </summary>
public static class ApplicationTemplates
{
    /// <summary>
    /// The starter files of the context's application, keyed by path relative to the project directory.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the application kind is unknown.</exception>
    public static IReadOnlyDictionary<string, string> Render(ApplicationContext context) =>
        context.App.Kind switch {
            ApplicationKind.Api => ApiTemplate.Render(context),
            ApplicationKind.Ui => UiTemplate.Render(context),
            ApplicationKind.Job => JobTemplate.Render(context),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.App.Kind, null)
        };

    /// <summary>
    /// A library project of a layer, referencing the layer below it unless <paramref name="lowerLayerReference"/>
    /// (relative to the library's directory) is null.
    /// </summary>
    public static string Library(string? lowerLayerReference) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        {{References(lowerLayerReference)}}</Project>

        """;

    /// <summary>
    /// A non-packable application project on <paramref name="sdk"/>, referencing its toolkit package and layer library.
    /// </summary>
    internal static string Project(string sdk, ApplicationContext context, bool exe) => $$"""
        <Project Sdk="{{sdk}}">
          <PropertyGroup>
        {{(exe ? "    <OutputType>Exe</OutputType>\n" : "")}}    <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <RootNamespace>{{context.Namespace}}</RootNamespace>
            <AssemblyName>{{context.Namespace}}</AssemblyName>
            <!-- An application, not a library: keep it out of NuGet packing. -->
            <IsPackable>false</IsPackable>
          </PropertyGroup>
          <ItemGroup>
            {{PackageReference(context.App.PackageId, context.PackageVersion)}}
          </ItemGroup>
        {{References(context.LibraryReference)}}</Project>

        """;

    /// <summary>
    /// A <c>PackageReference</c>, versioned unless <paramref name="version"/> is null (central package management).
    /// </summary>
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
