using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Core.Template;

/// <summary>
/// What an application template needs to render one project; <c>LibraryReference</c> is the path of its layer's
/// library relative to the project directory, if any.
/// </summary>
public sealed record ApplicationContext(
    ApplicationSpec App,
    string ProjectName,
    string Namespace,
    string PackageVersion,
    (int Http, int Https)? Ports,
    string? LibraryReference);

/// <summary>The starter files of each <see cref="ApplicationKind"/>, keyed by path relative to the project directory.</summary>
public static class ApplicationTemplates
{
    public static IReadOnlyDictionary<string, string> Render(ApplicationContext context) =>
        context.App.Kind switch {
            ApplicationKind.Api => ApiTemplate.Render(context),
            ApplicationKind.Ui => UiTemplate.Render(context),
            ApplicationKind.Job => JobTemplate.Render(context),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.App.Kind, null)
        };

    /// <summary>A library project of a layer, referencing the layer below it.</summary>
    public static string Library(string? lowerLayerReference) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        {{References(lowerLayerReference)}}</Project>

        """;

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
            <PackageReference Include="{{context.App.PackageId}}" Version="{{context.PackageVersion}}" />
          </ItemGroup>
        {{References(context.LibraryReference)}}</Project>

        """;

    private static string References(string? reference) => reference is null
        ? ""
        : $"""
            <ItemGroup>
              <ProjectReference Include="{reference.Replace('/', '\\')}" />
            </ItemGroup>

          """;

    internal static (int Http, int Https) PortsOf(ApplicationContext context) => context.Ports ?? (LocalPorts.First, LocalPorts.First + 1);

    internal static string Kebab(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
