using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Component;

public class BoilerplateTest
{
    [Fact]
    public void Create_GivenConflictingFile_LeavesExistingFilesUntouched()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "Makefile"), "mine");
            var create = () => BoilerplateTasks.Create(root, "MyApp");
            create.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*overwrite*");

            File.ReadAllText(Path.Combine(root, "Makefile"))
                .Should()
                .Be("mine");

            Directory.Exists(Path.Combine(root, "scripts"))
                .Should()
                .BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenConfiguredSolution_DoesNotReplaceParameters()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "build"));
            Directory.CreateDirectory(Path.Combine(root, ".fallout"));
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"), "<Project />");
            var parameters = Path.Combine(root, ".fallout", "parameters.json");
            File.WriteAllText(parameters, "{\"Solution\":\"existing.sln\"}");
            var create = () => BoilerplateTasks.Create(root, "MyApp");
            create.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*already defines a solution*");

            File.ReadAllText(parameters)
                .Should()
                .Be("{\"Solution\":\"existing.sln\"}");

            File.Exists(Path.Combine(root, "Makefile"))
                .Should()
                .BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenExistingSettings_PreservesOrAppendsWithoutDuplicating()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            var files = new Dictionary<string, string> {
                [".gitignore"] = "my-ignore",
                [".editorconfig"] = "root = false\n[*]\nindent_size = 8",
                ["global.json"] = "my-sdk",
                ["NuGet.Config"] = "my-feeds",
                ["Directory.Build.props"] = "my-props"
            };

            foreach (var (name, content) in files) File.WriteAllText(Path.Combine(root, name), content);
            var options = new BoilerplateOptions {
                Items = ".gitignore,.editorconfig,global.json,nuget.config,Directory.Build.props"
            };

            BoilerplateTasks.Create(root, null, options);
            foreach (var (name, content) in files)
                File.ReadAllText(Path.Combine(root, name))
                    .Should()
                    .Be(content);

            options.AppendExisting = true;
            BoilerplateTasks.Create(root, null, options);
            var gitignore = File.ReadAllText(Path.Combine(root, ".gitignore"));
            var editorconfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
            gitignore.Should()
                .StartWith("# >>> anarchy-toolkit")
                .And.Contain(".fallout/temp")
                .And.EndWith("my-ignore\n");

            gitignore.IndexOf(".fallout/temp", StringComparison.Ordinal)
                .Should()
                .BeLessThan(gitignore.IndexOf("my-ignore", StringComparison.Ordinal));

            editorconfig.Should()
                .StartWith("# >>> anarchy-toolkit")
                .And.Contain("root = true")
                .And.Contain("[*]\nindent_size = 8")
                .And.Contain("resharper_sort_usings")
                .And.NotContain("root = false");

            editorconfig.IndexOf("root = true", StringComparison.Ordinal)
                .Should()
                .BeLessThan(editorconfig.IndexOf("[*]", StringComparison.Ordinal));

            foreach (var name in new[] {
                         "global.json",
                         "NuGet.Config",
                         "Directory.Build.props"
                     })
                File.ReadAllText(Path.Combine(root, name))
                    .Should()
                    .Be(files[name]);

            Directory.EnumerateFiles(root)
                .Select(Path.GetFileName)
                .Should()
                .NotContain("nuget.config");

            BoilerplateTasks.Create(root, null, options);
            File.ReadAllText(Path.Combine(root, ".gitignore"))
                .Should()
                .Be(gitignore);

            File.ReadAllText(Path.Combine(root, ".editorconfig"))
                .Should()
                .Be(editorconfig);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenEditorConfigDryRun_ReportsTheManagedBlockBeforeOverridesWithoutWriting()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-editorconfig-" + Guid.NewGuid());
        const string existing = "[*]\nindent_size = 8\n";
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, ".editorconfig");
            File.WriteAllText(path, existing);
            var messages = new List<string>();

            SolutionScaffolder.Create(root, null, new ScaffoldOptions {
                Items = ".editorconfig",
                AppendExisting = true,
                DryRun = true,
                Log = messages.Add
            });

            var diff = messages.Should()
                .ContainSingle(message => message.StartsWith("Update the managed block of .editorconfig"))
                .Which;

            diff.IndexOf("+# >>> anarchy-toolkit", StringComparison.Ordinal)
                .Should()
                .BeLessThan(diff.IndexOf(" [*]", StringComparison.Ordinal));

            File.ReadAllText(path)
                .Should()
                .Be(existing);

            var comparison = BoilerplateCatalog.Compare(root, [".editorconfig"])
                .Should()
                .ContainSingle()
                .Which;

            comparison.Status.Should()
                .Be(BoilerplateCatalog.FileStatus.Different);

            comparison.Diff.IndexOf("+# >>> anarchy-toolkit", StringComparison.Ordinal)
                .Should()
                .BeLessThan(comparison.Diff.IndexOf(" [*]", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenExistingGlobalJsonAndAppendExisting_AddsOnlyMissingTestRunner()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "global.json");
            var options = new BoilerplateOptions {
                Items = "global.json",
                AppendExisting = true
            };

            File.WriteAllText(path, """{ "sdk": { "version": "9.0.100" } }""");
            BoilerplateTasks.Create(root, null, options);
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            json["sdk"]!["version"]!.GetValue<string>()
                .Should()
                .Be("9.0.100");

            json["test"]!["runner"]!.GetValue<string>()
                .Should()
                .Be("Microsoft.Testing.Platform");

            var merged = File.ReadAllText(path);
            BoilerplateTasks.Create(root, null, options);
            File.ReadAllText(path)
                .Should()
                .Be(merged);

            const string ownRunner = """{ "test": { "runner": "VSTest" } }""";
            File.WriteAllText(path, ownRunner);
            BoilerplateTasks.Create(root, null, options);
            File.ReadAllText(path)
                .Should()
                .Be(ownRunner);

            const string withComments = "{\n  // pinned\n  \"sdk\": { \"version\": \"9.0.100\" }\n}";
            File.WriteAllText(path, withComments);
            BoilerplateTasks.Create(root, null, options);
            File.ReadAllText(path)
                .Should()
                .Be(withComments);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenSelection_HandlesOnlyRequestedItems()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "existing.sln"), "existing");
            var options = new BoilerplateOptions {
                Items = "scripts/console,.gitignore,Directory.Build.props",
                Exclude = "scripts/console/banner-wide.sh,.gitignore"
            };

            BoilerplateTasks.Create(root, null, options);
            File.Exists(Path.Combine(root, "scripts", "console", "title.sh"))
                .Should()
                .BeTrue();

            File.Exists(Path.Combine(root, "scripts", "console", "banner-wide.sh"))
                .Should()
                .BeFalse();

            File.Exists(Path.Combine(root, ".gitignore"))
                .Should()
                .BeFalse();

            File.Exists(Path.Combine(root, "Directory.Build.props"))
                .Should()
                .BeTrue();

            File.Exists(Path.Combine(root, "Makefile"))
                .Should()
                .BeFalse();

            File.ReadAllText(Path.Combine(root, "existing.sln"))
                .Should()
                .Be("existing");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenUnknownItem_FailsBeforeWritingAnything()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            var create = () => BoilerplateTasks.Create(root, null, new BoilerplateOptions {
                Items = ".gitignore,not-an-item"
            });

            create.Should()
                .Throw<ArgumentException>()
                .WithMessage("*not-an-item*");

            Directory.EnumerateFileSystemEntries(root)
                .Should()
                .BeEmpty();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
