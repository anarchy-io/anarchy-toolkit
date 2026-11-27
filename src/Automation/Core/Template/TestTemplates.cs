namespace Anarchy.Toolkit.Automation.Core.Template;

/// <summary>
/// The files of a solution's test projects: the shared, non-executable common project, the projects of each test
/// kind and the mock server host.
/// </summary>
public static class TestTemplates
{
    /// <summary>The toolkit's base test kit, which the common project always references.</summary>
    public const string TestingPackage = "Anarchy.Toolkit.Testing";

    /// <summary>The test kit for APIs and UIs.</summary>
    public const string TestingWebPackage = "Anarchy.Toolkit.Testing.Runtime.Web";

    /// <summary>The test kit for the mock server.</summary>
    public const string TestingMockServerPackage = "Anarchy.Toolkit.Testing.MockServer";

    /// <summary>The package of a mock server host.</summary>
    public const string MockServerPackage = "Anarchy.Toolkit.Runtime.Web.MockServer.Ui";

    /// <summary>The name of the mock server host project, which <c>make mock_server</c> finds by its suffix.</summary>
    public const string MockServerProject = "MockServer";

    /// <summary>
    /// The common project's files, keyed by path relative to its directory.
    /// </summary>
    /// <param name="projectName">The project name, e.g. <c>Common</c>.</param>
    /// <param name="rootNamespace">The root namespace and assembly name.</param>
    /// <param name="packages">The test kits it references, each versioned unless its version is null.</param>
    /// <returns>The file contents.</returns>
    public static IReadOnlyDictionary<string, string> Common(string projectName, string rootNamespace,
        IEnumerable<(string Id, string? Version)> packages) => new Dictionary<string, string> {
            [$"{projectName}.csproj"] = ApplicationTemplates.Library(null, rootNamespace, packages)
            .Replace("  </PropertyGroup>", "    <IsPackable>false</IsPackable>\n    <IsTestProject>false</IsTestProject>\n  </PropertyGroup>"),
            ["README.md"] = $"""
            # {projectName}

            The code more than one test kind (unit, integration, end-to-end) can use: fixtures, mocks, fakes, helpers and
            custom assertions. Every test project references it.

            It is not a test project and runs nothing by itself: it builds on top of Anarchy's `Anarchy.Toolkit.Testing*`
            packages, which it references for the test projects, so that shared code can use their test cases, fixtures
            and helpers.

            Keep code that only one test kind uses in that test project.

            """
        };

    /// <summary>
    /// A test kind's project, referencing the common project.
    /// </summary>
    /// <param name="rootNamespace">The root namespace and assembly name.</param>
    /// <param name="commonReference">The common project's path relative to the test project's directory, if any.</param>
    /// <returns>The <c>.csproj</c> content.</returns>
    public static string TestProject(string rootNamespace, string? commonReference) =>
        ApplicationTemplates.Library(commonReference, rootNamespace);

    /// <summary>
    /// The mock server host's files, keyed by path relative to its directory.
    /// </summary>
    /// <param name="rootNamespace">The root namespace and assembly name.</param>
    /// <param name="version">The version of <see cref="MockServerPackage"/>, or <see langword="null"/> under central package management.</param>
    /// <returns>The file contents.</returns>
    public static IReadOnlyDictionary<string, string> MockServer(string rootNamespace, string? version) => new Dictionary<string, string> {
        [$"{MockServerProject}.csproj"] = $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RootNamespace>{{rootNamespace}}</RootNamespace>
                <AssemblyName>{{rootNamespace}}</AssemblyName>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                {{ApplicationTemplates.PackageReference(MockServerPackage, version)}}
              </ItemGroup>
              <ItemGroup>
                <!-- Copied next to the assembly, so the default expectations are found wherever it runs from. -->
                <None Include="mocks\**\*.json" CopyToOutputDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>

            """,
        ["EntryPointLocal.cs"] = $$"""
            using Anarchy.Toolkit.Runtime.Web.MockServer;
            using Anarchy.Toolkit.Runtime.Web.MockServer.Ui.Extensions.Runtime.Web.MockServer;

            namespace {{rootNamespace}};

            public static class EntryPointLocal
            {
                public static async Task<int> Main(string[] args) => await MockServerEntryPoint
                    .RunAsync(args, server => server
                        .AddExpectationsFrom(Path.Combine(AppContext.BaseDirectory, "mocks"))
                        .UseUi());
            }

            """,
        ["mocks/ping.json"] = """
            [
              {
                "id": "ping",
                "httpRequest": { "method": "GET", "path": "/ping" },
                "httpResponse": { "statusCode": 200, "body": { "message": "pong" } }
              }
            ]

            """
    };
}
