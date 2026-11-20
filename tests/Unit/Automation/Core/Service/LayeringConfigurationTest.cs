using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Service;

public class LayeringConfigurationTest
{
    private static SolutionLayout Parse(string json) => LayeringConfiguration.Parse(json, "preferences.json");

    private static string Errors(string json)
    {
        var act = () => Parse(json);
        return act.Should().Throw<InvalidOperationException>().Which.Message;
    }

    [Fact]
    public void Parse_GivenNoLayeringOverrides_UsesTheInCodeFolderLayout()
    {
        foreach (var json in new[] { "{}", "{ \"layering\": {} }" })
            Parse(json).Should().BeEquivalentTo(SolutionLayout.Default);
    }

    [Fact]
    public void Parse_GivenPartialFolderOverrides_ResolvesTheUnspecifiedFields()
    {
        var layout = Parse("""{ "layering": { "root": "code" } }""");

        layout.Mode.Should().Be(LayeringMode.Folder);
        layout.Root.Should().Be("code");
        layout.LibraryRoot.Should().Be("code/Library");
        layout.ApplicationFolders.Should().BeEquivalentTo(new Dictionary<string, string> {
            ["code/Api"] = "Web",
            ["code/Jobs"] = "Hosting",
            ["code/Ui"] = "Web"
        });
    }

    [Fact]
    public void Parse_GivenPrefixMode_DefaultsLayersAndRootWithoutFolderSettings()
    {
        var layout = Parse("""{ "layering": { "mode": "prefix" } }""");

        layout.Should().BeEquivalentTo(new SolutionLayout(LayeringMode.Prefix, SolutionLayout.DefaultLayers));
        layout.Root.Should().Be(SolutionLayout.DefaultRoot);
        layout.LibraryRoot.Should().BeNull();
        layout.ApplicationFolders.Should().BeEmpty();
    }

    [Fact]
    public void Parse_GivenPartialFolderPreferencesAndExplicitModeChange_DoesNotInheritFolderOnlyValues()
    {
        var preferences = AutomationPreferences.Parse("""
            {
              "layering": {
                "mode": "folder",
                "libraryRoot": "src/custom-library",
                "applicationFolders": { "src/services": "Web" }
              }
            }
            """);

        var layout = preferences.ResolveLayout(new JsonObject { ["mode"] = "prefix" });

        layout.Mode.Should().Be(LayeringMode.Prefix);
        layout.LibraryRoot.Should().BeNull();
        layout.ApplicationFolders.Should().BeEmpty();
        layout.Layers.Should().Equal(SolutionLayout.DefaultLayers);
    }

    [Fact]
    public void ToJson_RoundTripsFolderAndPrefixLayoutsAsNestedOverrides()
    {
        var prefix = new SolutionLayout(LayeringMode.Prefix, ["Core", "Web"]);
        var single = new SolutionLayout(LayeringMode.Folder, ["Core", "Web"]) {
            LibraryRoot = "src",
            ApplicationFolders = new Dictionary<string, string> { ["src/App"] = "Web" }
        };

        foreach (var layout in new[] { SolutionLayout.Default, prefix, single })
            Parse(LayeringConfiguration.ToJson(layout)).Should().BeEquivalentTo(layout);

        var json = JsonNode.Parse(LayeringConfiguration.ToJson(prefix))!.AsObject();
        json["$schema"]!.GetValue<string>().Should().Be(LayeringConfiguration.SchemaUrl);
        json["layering"]!["mode"]!.GetValue<string>().Should().Be("prefix");
        json.ContainsKey("mode").Should().BeFalse();
        json["layering"]!.AsObject().ContainsKey("libraryRoot").Should().BeFalse();
        json["layering"]!.AsObject().ContainsKey("applicationFolders").Should().BeFalse();
    }

    [Theory]
    [InlineData("[]", "expected an object")]
    [InlineData("{", "invalid JSON")]
    [InlineData("""{ "layering": { "mode": "Folder" } }""", "expected one of")]
    [InlineData("""{ "layering": { "mode": "both" } }""", "expected one of")]
    [InlineData("""{ "layering": { "layeringByFolder": true } }""", "unknown property")]
    [InlineData("""{ "layering": { "layers": "Core Web" } }""", "expected array")]
    [InlineData("""{ "layering": { "layers": ["Core", 1] } }""", "expected string")]
    [InlineData("""{ "docker": { "timeout": 0 } }""", "outside the allowed range")]
    [InlineData("""{ "mockServer": { "port": 65536 } }""", "outside the allowed range")]
    [InlineData("""{ "unlisted": true }""", "unknown property")]
    [InlineData("""{ "build": { "password": "secret" } }""", "unknown property")]
    [InlineData("""{ "layering": { "mode": "prefix", "libraryRoot": "src/Library" } }""", "one layering mode")]
    [InlineData("""{ "layering": { "mode": "prefix", "applicationFolders": {} } }""", "one layering mode")]
    [InlineData("""{ "layering": { "mode": "folder", "root": "/abs" } }""", "inside the solution")]
    [InlineData("""{ "layering": { "mode": "folder", "root": "\\\\server\\share" } }""", "inside the solution")]
    [InlineData("""{ "layering": { "mode": "folder", "root": "C:/outside" } }""", "inside the solution")]
    [InlineData("""{ "layering": { "mode": "folder", "root": "src/../../outside" } }""", "inside the solution")]
    [InlineData("""{ "layering": { "mode": "folder", "root": "../shared" } }""", "inside the solution")]
    [InlineData("""{ "layering": { "layers": ["Core", "core"] } }""", "listed more than once")]
    [InlineData("""{ "layering": { "layers": ["Core", "Testing"] } }""", "reserved")]
    public void Parse_GivenInvalidPreferences_DiagnosesSourceAndProperty(string json, string expected)
    {
        Errors(json).Should().Contain(expected).And.Contain("preferences.json");
    }

    [Fact]
    public void Parse_GivenCommentsAndTrailingCommas_AcceptsJsonc()
    {
        var layout = Parse("""
            {
              // Optional settings can be documented in-place.
              "layering": {
                "mode": "prefix",
                "layers": ["Core", "Web",],
              },
            }
            """);

        layout.Mode.Should().Be(LayeringMode.Prefix);
        layout.Layers.Should().Equal("Core", "Web");
    }

    [Fact]
    public void Parse_GivenDuplicateProperties_RejectsThemWithSourceDiagnostic()
    {
        Errors("""{ "build": { "configuration": "Debug", "configuration": "Release" } }""")
            .Should().Contain("invalid JSON").And.Contain("preferences.json");
    }

    [Theory]
    [InlineData("""{ "layering": { "layers": [] } }""", "at least one layer")]
    [InlineData("""{ "layering": { "libraryRoot": "lib" } }""", "outside root")]
    [InlineData("""{ "layering": { "applicationFolders": { "src/Api": "Missing" } } }""", "not one of the layers")]
    [InlineData("""{ "layering": { "applicationFolders": { "src/Api": "Web", "src/Api/Admin": "Web" } } }""", "overlap")]
    public void Parse_GivenInvalidEffectiveLayout_RejectsIt(string json, string expected)
    {
        Errors(json).Should().Contain(expected);
    }

    [Fact]
    public void Read_GivenMissingFile_UsesDefaultsWithoutCreatingConfiguration()
    {
        using var root = new TemporaryRoot();

        LayeringConfiguration.Read(root.Path).Should().BeEquivalentTo(SolutionLayout.Default);
        File.Exists(Path.Combine(root.Path, AutomationPreferences.FileName)).Should().BeFalse();
    }

    [Fact]
    public void Write_CreatesTheUnifiedFileOnceAndRejectsInvalidLayouts()
    {
        using var root = new TemporaryRoot();
        LayeringConfiguration.Write(root.Path, SolutionLayout.Default);

        LayeringConfiguration.Read(root.Path).Should().BeEquivalentTo(SolutionLayout.Default);
        File.ReadAllText(Path.Combine(root.Path, AutomationPreferences.FileName)).Should().Contain("\"layering\"");

        var again = () => LayeringConfiguration.Write(root.Path, SolutionLayout.Default);
        again.Should().Throw<IOException>();

        var invalidLayout = SolutionLayout.Default with { Layers = [] };
        var invalid = () => LayeringConfiguration.Write(Path.Combine(root.Path, "other"), invalidLayout);
        invalid.Should().Throw<InvalidOperationException>().WithMessage("*at least one layer*");
        Directory.Exists(Path.Combine(root.Path, "other")).Should().BeFalse();
    }

    [Fact]
    public void PublishedSchema_DeclaresOptionalSectionsAndMatchesTheRuntimeSchemaId()
    {
        var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "schemas", "anarchy-toolkit.schema.json")))!;

        schema["$id"]!.GetValue<string>().Should().Be(AutomationPreferences.SchemaUrl);
        ContainsRequired(schema).Should().BeFalse();
        schema["properties"]!.AsObject().Select(property => property.Key)
            .Should().Contain("layering").And.Contain("build").And.Contain("docker");
        schema["properties"]!["layering"]!["properties"]!["layers"]!["default"]!.AsArray()
            .Select(layer => layer!.GetValue<string>()).Should().Equal(SolutionLayout.DefaultLayers);
        LayeringTasks.DefaultLayers.Should().Equal(SolutionLayout.DefaultLayers);
    }

    private static bool ContainsRequired(JsonNode node) => node switch {
        JsonObject obj => obj.ContainsKey("required") || obj.Any(pair => ContainsRequired(pair.Value!)),
        JsonArray array => array.Any(item => item is not null && ContainsRequired(item)),
        _ => false
    };

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "..", ".."));

    private sealed class TemporaryRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "anarchy-layering-" + Guid.NewGuid().ToString("N"));
        public TemporaryRoot() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
