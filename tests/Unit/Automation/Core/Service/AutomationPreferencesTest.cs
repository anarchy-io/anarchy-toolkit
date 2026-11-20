using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Service;

public class AutomationPreferencesTest
{
    [Fact]
    public void Read_GivenMissingFile_ReturnsEmptyOverridesAndDoesNotCreateIt()
    {
        using var root = new TemporaryRoot();

        var preferences = AutomationPreferences.Read(root.Path);

        preferences.Saved.Should().BeEmpty();
        preferences.Get<string>("build.configuration").Should().BeNull();
        preferences.ResolveLayout().Should().BeEquivalentTo(SolutionLayout.Default);
        preferences.Describe().Should().Contain("effectiveLayering");
        File.Exists(Path.Combine(root.Path, AutomationPreferences.FileName)).Should().BeFalse();
    }

    [Fact]
    public void Parse_GivenEmptyAndPartialDocuments_DoesNotMaterializeDefaults()
    {
        foreach (var json in new[] { "{}", "{ \"docker\": {} }", "{ \"build\": { \"configuration\": \"Debug\" } }" })
        {
            var preferences = AutomationPreferences.Parse(json);
            JsonNode.DeepEquals(preferences.Saved, JsonNode.Parse(json)).Should().BeTrue();
            preferences.Saved.AsObject().Should().NotContainKey("layering");
            preferences.Get<string>("build.sourceRoot").Should().BeNull();
        }

        AutomationPreferences.Parse("{ \"build\": { \"configuration\": \"Debug\" } }")
            .Get<string>("build.configuration").Should().Be("Debug");
    }

    [Fact]
    public void Describe_LabelsSavedDefaultsAndInvocationValues()
    {
        var preferences = AutomationPreferences.Parse("""
            { "build": { "configuration": "Debug" } }
            """);
        var layout = new SolutionLayout(LayeringMode.Prefix, ["Core", "Web"]);
        var description = JsonNode.Parse(preferences.Describe(new Dictionary<string, (JsonNode? Value, string Source)> {
            ["layering.mode"] = (JsonValue.Create("prefix"), "Fallout parameter")
        }, layout))!;

        description["effective"]!["build.configuration"]!["source"]!.GetValue<string>().Should().Be("saved value");
        description["effective"]!["build.configuration"]!["value"]!.GetValue<string>().Should().Be("Debug");
        description["effective"]!["build.treatWarningsAsErrors"]!["source"]!.GetValue<string>().Should().Be("in-code default");
        description["effective"]!["build.treatWarningsAsErrors"]!["value"]!.GetValue<bool>().Should().BeTrue();
        description["effective"]!["layering.mode"]!["source"]!.GetValue<string>().Should().Be("Fallout parameter");
        description["effectiveLayering"]!["mode"]!.GetValue<string>().Should().Be("prefix");
    }

    [Fact]
    public void Parse_GivenCommentsAndTrailingCommas_AcceptsOptionalJsoncDocument()
    {
        var preferences = AutomationPreferences.Parse("""
            {
              // Keep choices that differ from toolkit behavior only.
              "docker": { "waitFor": ["postgres",], },
            }
            """);

        preferences.Get<string[]>("docker.waitFor").Should().Equal("postgres");
        preferences.Saved["docker"]!["timeout"].Should().BeNull();
    }

    [Theory]
    [InlineData("[]", "expected an object")]
    [InlineData("{", "invalid JSON")]
    [InlineData("""{ "build": { "configuration": 1 } }""", "expected string")]
    [InlineData("""{ "gitHooks": { "skipLintFix": "false" } }""", "expected boolean")]
    [InlineData("""{ "docker": { "waitFor": "postgres" } }""", "expected array")]
    [InlineData("""{ "build": { "unknown": true } }""", "unknown property")]
    [InlineData("""{ "secret": "do-not-save" }""", "unknown property")]
    [InlineData("""{ "docker": { "timeout": 0 } }""", "outside the allowed range")]
    [InlineData("""{ "mockServer": { "port": 65536 } }""", "outside the allowed range")]
    [InlineData("""{ "agentSkills": { "directories": ["../outside"] } }""", "inside the solution")]
    [InlineData("""{ "agentSkills": { "directories": ["skills/../../outside"] } }""", "inside the solution")]
    [InlineData("""{ "agentSkills": { "directories": ["/outside"] } }""", "inside the solution")]
    [InlineData("""{ "agentSkills": { "directories": ["C:/outside"] } }""", "inside the solution")]
    public void Parse_GivenInvalidValues_ReportsTheFileAndProperty(string json, string expected)
    {
        var act = () => AutomationPreferences.Parse(json, "sample-preferences.json");

        act.Should().Throw<InvalidOperationException>().Which.Message
            .Should().Contain("sample-preferences.json").And.Contain(expected);
    }

    [Fact]
    public void Parse_GivenDuplicateProperties_RejectsRatherThanChoosingOne()
    {
        var act = () => AutomationPreferences.Parse("""{ "build": { "configuration": "Debug", "configuration": "Release" } }""");

        act.Should().Throw<InvalidOperationException>().WithMessage("*invalid JSON*");
    }

    [Fact]
    public void With_GivenFalseAndEmptyCollection_PreservesExplicitClearValues()
    {
        var preferences = AutomationPreferences.Parse("""
            { "gitHooks": { "skipTestRuntime": true }, "docker": { "waitFor": ["postgres"] } }
            """);

        var edited = preferences.With("gitHooks.skipTestRuntime", "false")
            .With("docker.waitFor", "[]");

        edited.Get<bool?>("gitHooks.skipTestRuntime").Should().Be(false);
        edited.Get<string[]>("docker.waitFor").Should().BeEmpty();
        edited.Saved["build"].Should().BeNull();
    }

    [Fact]
    public void With_GivenSectionOnlyPath_RejectsItAndRequiresAPropertyKey()
    {
        foreach (var key in new[] { "build", "docker" })
        {
            var act = () => AutomationPreferences.Parse("{}").With(key, null);
            act.Should().Throw<ArgumentException>().WithMessage("*preference path*");
        }
    }

    [Fact]
    public void WithLayering_MergesSparseChangesAndClearsFolderFieldsOnModeChange()
    {
        var preferences = AutomationPreferences.Parse("""
            {
              "build": { "sourceRoot": "production" },
              "layering": {
                "mode": "folder",
                "layers": ["Core", "Web"],
                "root": "code",
                "libraryRoot": "code/Library",
                "applicationFolders": { "code/Api": "Web" }
              }
            }
            """);

        var edited = preferences.WithLayering(new JsonObject {
            ["mode"] = "prefix",
            ["layers"] = new JsonArray("Core", "Web", "Automation")
        });

        edited.Get<string>("build.sourceRoot").Should().Be("production");
        edited.Get<string>("layering.root").Should().Be("code");
        edited.Get<string[]>("layering.layers").Should().Equal("Core", "Web", "Automation");
        edited.Saved["layering"]!["libraryRoot"].Should().BeNull();
        edited.Saved["layering"]!["applicationFolders"].Should().BeNull();
        edited.ResolveLayout().Mode.Should().Be(LayeringMode.Prefix);
    }

    [Fact]
    public void WithLayering_GivenOmittedSavedModeAndFolderModeEdit_PreservesFolderOverrides()
    {
        var preferences = AutomationPreferences.Parse("""
            { "layering": { "libraryRoot": "src/custom", "applicationFolders": { "src/Api": "Web" } } }
            """);

        var edited = preferences.WithLayering(new JsonObject { ["mode"] = "folder" });

        edited.Saved["layering"]!["libraryRoot"]!.GetValue<string>().Should().Be("src/custom");
        edited.Saved["layering"]!["applicationFolders"]!["src/Api"]!.GetValue<string>().Should().Be("Web");
        edited.ResolveLayout().Mode.Should().Be(LayeringMode.Folder);
    }

    [Fact]
    public void WithLayering_GivenSparseEdit_DoesNotPersistEffectiveDefaults()
    {
        var edited = AutomationPreferences.Parse("{}").WithLayering(new JsonObject { ["mode"] = "prefix" });

        edited.Saved["layering"]!.AsObject().Select(property => property.Key).Should().Equal("mode");
        edited.ResolveLayout().Layers.Should().Equal(SolutionLayout.DefaultLayers);
    }

    [Theory]
    [InlineData("unknown.preference")]
    [InlineData("build.configuration.secret")]
    [InlineData("$schema")]
    [InlineData("container.registryPassword")]
    public void With_GivenUnknownOrNonPersistableKey_RejectsIt(string key)
    {
        var act = () => AutomationPreferences.Parse("{}").With(key, "\"value\"");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("build.configuration", "null")]
    [InlineData("docker.timeout", "0")]
    [InlineData("gitHooks.skipTestStatic", "\"yes\"")]
    public void With_GivenInvalidTypedValue_RejectsBeforeAnyWrite(string key, string json)
    {
        using var root = new TemporaryRoot();
        var path = Path.Combine(root.Path, AutomationPreferences.FileName);
        const string existing = "{ \"build\": { \"configuration\": \"Debug\" } }";
        File.WriteAllText(path, existing);

        var act = () => AutomationPreferences.Read(root.Path).With(key, json).Save(root.Path);

        act.Should().Throw<InvalidOperationException>();
        File.ReadAllText(path).Should().Be(existing);
        Directory.GetFiles(root.Path, "*.tmp").Should().BeEmpty();
    }

    [Fact]
    public void WithEdits_ValidatesTheFinalCombinationRatherThanIntermediateStates()
    {
        var preferences = AutomationPreferences.Parse("""
            { "layering": { "layers": ["Domain", "Presentation"], "applicationFolders": { "src/App": "Presentation" } } }
            """);

        var edited = preferences.WithEdits(new Dictionary<string, string?> {
            ["layering.layers"] = null,
            ["layering.applicationFolders"] = null
        });

        edited.ResolveLayout().Should().BeEquivalentTo(SolutionLayout.Default);
        edited.Saved.Should().BeEmpty();
        preferences.ResolveLayout().Layers.Should().Equal("Domain", "Presentation");
    }

    [Fact]
    public void Unset_WhenNoFileExists_DoesNotCreateAnEmptyConfiguration()
    {
        using var root = new TemporaryRoot();

        AutomationPreferences.Read(root.Path).With("docker.timeout", null).Save(root.Path);

        File.Exists(Path.Combine(root.Path, AutomationPreferences.FileName)).Should().BeFalse();
    }

    [Fact]
    public void WithAndSave_MergesTheRequestedKeyAndUnsetRestoresFallback()
    {
        using var root = new TemporaryRoot();
        var path = Path.Combine(root.Path, AutomationPreferences.FileName);
        File.WriteAllText(path, """
            { "build": { "configuration": "Debug" }, "docker": { "projectName": "test-db" } }
            """);

        AutomationPreferences.Read(root.Path).With("docker.timeout", "42").Save(root.Path);
        var saved = AutomationPreferences.Read(root.Path);
        saved.Get<string>("build.configuration").Should().Be("Debug");
        saved.Get<string>("docker.projectName").Should().Be("test-db");
        saved.Get<int?>("docker.timeout").Should().Be(42);
        saved.Saved["$schema"]!.GetValue<string>().Should().Be(AutomationPreferences.SchemaUrl);

        saved.With("docker.timeout", null).Save(root.Path);
        var unset = AutomationPreferences.Read(root.Path);
        unset.Get<int?>("docker.timeout").Should().BeNull();
        unset.Get<string>("docker.projectName").Should().Be("test-db");
        unset.Get<string>("build.configuration").Should().Be("Debug");
    }

    [Fact]
    public void Read_GivenMalformedExistingFile_LeavesItUntouched()
    {
        using var root = new TemporaryRoot();
        var path = Path.Combine(root.Path, AutomationPreferences.FileName);
        const string malformed = "{ definitely not json";
        File.WriteAllText(path, malformed);

        var act = () => AutomationPreferences.Read(root.Path);

        act.Should().Throw<InvalidOperationException>();
        File.ReadAllText(path).Should().Be(malformed);
    }

    [Fact]
    public void Read_GivenDifferentRoots_DoesNotLeakPreferencesBetweenSolutions()
    {
        using var first = new TemporaryRoot();
        using var second = new TemporaryRoot();
        File.WriteAllText(Path.Combine(first.Path, AutomationPreferences.FileName), """{ "build": { "configuration": "Debug" } }""");
        File.WriteAllText(Path.Combine(second.Path, AutomationPreferences.FileName), """{ "build": { "configuration": "Release" } }""");

        AutomationPreferences.Read(first.Path).Get<string>("build.configuration").Should().Be("Debug");
        AutomationPreferences.Read(second.Path).Get<string>("build.configuration").Should().Be("Release");
        AutomationPreferences.Read(Path.Combine(second.Path, "missing")).Saved.Should().BeEmpty();
    }

    [Fact]
    public void PublishedSchemaDefaults_MatchTheReviewedDefaultContractFixture()
    {
        var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "schemas", "anarchy-toolkit.schema.json")))!;
        var fixture = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "tests", "Unit", "Automation", "Core", "Service", "AutomationPreferencesDefaults.json")));
        var actual = new JsonObject();

        AddDefaults(schema, "", actual);

        JsonNode.DeepEquals(actual, fixture).Should().BeTrue();
        AutomationPreferences.Read(Path.Combine(RepositoryRoot(), "does-not-exist"))
            .ResolveLayout().Should().BeEquivalentTo(SolutionLayout.Default);
    }

    private static void AddDefaults(JsonNode schema, string prefix, JsonObject result)
    {
        if (schema["default"] is { } defaultValue)
            result[prefix] = defaultValue.DeepClone();
        if (schema["properties"] is not JsonObject properties) return;
        foreach (var (key, value) in properties)
            AddDefaults(value!, prefix.Length == 0 ? key : prefix + "." + key, result);
    }

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "..", ".."));

    private sealed class TemporaryRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "anarchy-preferences-" + Guid.NewGuid().ToString("N"));
        public TemporaryRoot() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
