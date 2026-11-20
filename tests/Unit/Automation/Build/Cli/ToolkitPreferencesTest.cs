using Anarchy.Toolkit.Automation.Build.Cli.Service;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Cli;

public class ToolkitPreferencesTest
{
    private static readonly ToolkitPackageVersions Versions = new(new Dictionary<string, string> {
        ["Anarchy.Toolkit.Automation.Build"] = "1.2.3",
        ["Anarchy.Toolkit.Web"] = "2.0.0",
        ["Anarchy.Toolkit.Web.Ui"] = "2.1.0",
        ["Anarchy.Toolkit.Hosting"] = "3.0.0"
    });

    [Fact]
    public void ConfigShow_FindsTheRootUpwardsAndDoesNotCreatePreferences()
    {
        using var root = new TemporaryRoot();
        var workingDirectory = Directory.CreateDirectory(Path.Combine(root.Path, "src", "Library")).FullName;
        root.File(".fallout/parameters.json", """{ "Solution": "MyApp.sln" }""");
        var output = new StringWriter();
        var application = new ToolApplication(new ScriptedPrompter(), Versions, workingDirectory, output);

        application.Run(["config", "show"], interactive: false).Should().Be(0);

        output.ToString().Should().Contain("\"saved\": {}").And.Contain("effectiveLayering");
        var defaults = JsonNode.Parse(output.ToString())!;
        defaults["effective"]!["build.configuration"]!["source"]!.GetValue<string>().Should().Be("in-code default");
        defaults["effective"]!["build.configuration"]!["value"]!.GetValue<string>().Should().Be("Release");
        File.Exists(Path.Combine(root.Path, AutomationPreferences.FileName)).Should().BeFalse();

        root.File(AutomationPreferences.FileName, """{ "build": { "configuration": "Debug" } }""");
        var savedOutput = new StringWriter();
        new ToolApplication(new ScriptedPrompter(), Versions, workingDirectory, savedOutput)
            .Run(["config", "show"], interactive: false).Should().Be(0);
        savedOutput.ToString().Should().Contain("\"configuration\": \"Debug\"").And.Contain("effectiveLayering");
        var saved = JsonNode.Parse(savedOutput.ToString())!;
        saved["effective"]!["build.configuration"]!["source"]!.GetValue<string>().Should().Be("saved value");
        saved["effective"]!["build.configuration"]!["value"]!.GetValue<string>().Should().Be("Debug");
    }

    [Fact]
    public void ConfigSetAndUnset_MergeTypedValuesAndRestoreDefaultsFromTheDiscoveredRoot()
    {
        using var root = new TemporaryRoot();
        var workingDirectory = Directory.CreateDirectory(Path.Combine(root.Path, "build", "nested")).FullName;
        root.File(".fallout/parameters.json", """{ "Solution": "MyApp.sln" }""");
        const string existing = """{ "build": { "configuration": "Debug" }, "docker": { "projectName": "test-db" } }""";
        root.File(AutomationPreferences.FileName, existing);
        var prompter = new ScriptedPrompter();
        var application = new ToolApplication(prompter, Versions, workingDirectory);

        application.Run(["config", "set", "docker.timeout", "45"], interactive: false).Should().Be(0);
        var set = AutomationPreferences.Read(root.Path);
        set.Get<int?>("docker.timeout").Should().Be(45);
        set.Get<string>("build.configuration").Should().Be("Debug");
        set.Get<string>("docker.projectName").Should().Be("test-db");

        application.Run(["config", "unset", "build.configuration"], interactive: false).Should().Be(0);
        var unset = AutomationPreferences.Read(root.Path);
        unset.Get<string>("build.configuration").Should().BeNull();
        unset.Get<int?>("docker.timeout").Should().Be(45);
        unset.Get<string>("docker.projectName").Should().Be("test-db");
    }

    [Fact]
    public void ConfigConfigure_PreviewsAndRefusesByDefaultThenSavesOnlyAfterConsent()
    {
        using var root = new TemporaryRoot();
        root.File(".fallout/parameters.json", """{ "Solution": "MyApp.sln" }""");
        const string existing = "{ \"build\": { \"configuration\": \"Debug\" } }";
        var path = root.File(AutomationPreferences.FileName, existing);
        var declined = new ScriptedPrompter(new object[] { "docker.timeout" }, "45", false);

        new ToolApplication(declined, Versions, root.Path).Run(["config", "configure"], interactive: true).Should().Be(0);

        declined.Confirmations.Should().ContainSingle().Which.DefaultValue.Should().BeFalse();
        declined.Messages.Should().Contain(message => message.Contains("Proposed preferences"));
        declined.Messages.Should().Contain(message => message.Contains("no preferences were written"));
        System.IO.File.ReadAllText(path).Should().Be(existing);
        AutomationPreferences.Read(root.Path).Get<int?>("docker.timeout").Should().BeNull();
        AutomationPreferences.Read(root.Path).Get<string>("build.configuration").Should().Be("Debug");

        var accepted = new ScriptedPrompter(new object[] { "docker.timeout", "gitHooks.skipTestRuntime" }, "45", "false", true);
        new ToolApplication(accepted, Versions, root.Path).Run(["config", "configure"], interactive: true).Should().Be(0);

        accepted.Confirmations.Should().ContainSingle().Which.DefaultValue.Should().BeFalse();
        AutomationPreferences.Read(root.Path).Get<int?>("docker.timeout").Should().Be(45);
        AutomationPreferences.Read(root.Path).Get<bool?>("gitHooks.skipTestRuntime").Should().BeFalse();
        AutomationPreferences.Read(root.Path).Get<string>("build.configuration").Should().Be("Debug");
    }

    [Fact]
    public void ConfigSet_RejectsUnknownAndInvalidValuesWithoutChangingTheFile()
    {
        using var root = new TemporaryRoot();
        root.File(".fallout/parameters.json", """{ "Solution": "MyApp.sln" }""");
        const string existing = "{ \"docker\": { \"projectName\": \"test-db\" } }";
        var path = root.File(AutomationPreferences.FileName, existing);
        var prompter = new ScriptedPrompter();
        var application = new ToolApplication(prompter, Versions, root.Path);

        application.Run(["config", "set", "unknown.secret", "\"value\""], interactive: false).Should().Be(1);
        File.ReadAllText(path).Should().Be(existing);
        application.Run(["config", "set", "docker.timeout", "0"], interactive: false).Should().Be(1);
        File.ReadAllText(path).Should().Be(existing);
        prompter.Errors.Should().Contain(message => message.Contains("Unknown preference"));
        prompter.Errors.Should().Contain(message => message.Contains("outside the allowed range"));
    }

    [Fact]
    public void ConfigSet_GivenMalformedExistingFile_ReportsErrorAndLeavesItUntouched()
    {
        using var root = new TemporaryRoot();
        root.File(".fallout/parameters.json", """{ "Solution": "MyApp.sln" }""");
        const string malformed = "{ not json";
        var path = root.File(AutomationPreferences.FileName, malformed);
        var prompter = new ScriptedPrompter();

        new ToolApplication(prompter, Versions, root.Path)
            .Run(["config", "set", "docker.timeout", "45"], interactive: false).Should().Be(1);

        File.ReadAllText(path).Should().Be(malformed);
        prompter.Errors.Should().Contain(message => message.Contains("invalid JSON"));
    }

    private sealed class TemporaryRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "anarchy-cli-preferences-" + Guid.NewGuid().ToString("N"));
        public TemporaryRoot() => Directory.CreateDirectory(Path);

        public string File(string relativePath, string content)
        {
            var path = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.WriteAllText(path, content);
            return path;
        }

        public void Dispose() => Directory.Delete(Path, true);
    }
}
