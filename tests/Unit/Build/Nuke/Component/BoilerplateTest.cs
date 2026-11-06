using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using AwesomeAssertions;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class BoilerplateTest
{
    [Fact]
    public void Create_GivenBuildProject_CreatesUsableScaffold()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "build"));
            Directory.CreateDirectory(Path.Combine(root, ".nuke"));
            File.WriteAllText(Path.Combine(root, ".nuke", "parameters.json"), "{\"Configuration\":\"Debug\"}");
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

            BoilerplateTasks.Create(root, "MyApp");

            File.Exists(Path.Combine(root, "MyApp.sln")).Should().BeTrue();
            File.ReadAllText(Path.Combine(root, "MyApp.sln")).Should().Contain("Core.csproj").And.Contain("_build.csproj");
            File.Exists(Path.Combine(root, "Makefile")).Should().BeTrue();
            File.Exists(Path.Combine(root, "scripts", "make-targets", "code_quality.mk")).Should().BeTrue();
            File.Exists(Path.Combine(root, "scripts", "utils", "ascii_art-narrow.sh")).Should().BeTrue();
            File.Exists(Path.Combine(root, "scripts", "utils", "ascii_art-wide.sh")).Should().BeTrue();
            File.Exists(Path.Combine(root, "tests", ".gitkeep")).Should().BeTrue();
            File.ReadAllText(Path.Combine(root, ".gitignore")).Should().Contain(".nuke/temp");
            File.ReadAllText(Path.Combine(root, ".editorconfig")).Should().Contain("root = true");
            File.ReadAllText(Path.Combine(root, "global.json")).Should().Contain("10.0.0");
            File.ReadAllText(Path.Combine(root, "nuget.config")).Should().Contain("api.nuget.org");
            File.ReadAllText(Path.Combine(root, "Directory.Build.props")).Should().Contain("**\\Queries\\**\\*.sql");
            var parameters = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".nuke", "parameters.json")))!;
            parameters["Solution"]!.GetValue<string>().Should().Be("MyApp.sln");
            parameters["Configuration"]!.GetValue<string>().Should().Be("Debug");
            if (!OperatingSystem.IsWindows())
                File.GetUnixFileMode(Path.Combine(root, "scripts", "build.nuke.sh")).Should().HaveFlag(UnixFileMode.UserExecute);

            var repeat = () => BoilerplateTasks.Create(root, "MyApp");
            repeat.Should().Throw<InvalidOperationException>().WithMessage("*overwrite*");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenConflictingFile_LeavesExistingFilesUntouched()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "Makefile"), "mine");
            var create = () => BoilerplateTasks.Create(root, "MyApp");
            create.Should().Throw<InvalidOperationException>().WithMessage("*overwrite*");
            File.ReadAllText(Path.Combine(root, "Makefile")).Should().Be("mine");
            Directory.Exists(Path.Combine(root, "scripts")).Should().BeFalse();
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
            Directory.CreateDirectory(Path.Combine(root, ".nuke"));
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"), "<Project />");
            var parameters = Path.Combine(root, ".nuke", "parameters.json");
            File.WriteAllText(parameters, "{\"Solution\":\"existing.sln\"}");
            var create = () => BoilerplateTasks.Create(root, "MyApp");
            create.Should().Throw<InvalidOperationException>().WithMessage("*already defines a solution*");
            File.ReadAllText(parameters).Should().Be("{\"Solution\":\"existing.sln\"}");
            File.Exists(Path.Combine(root, "Makefile")).Should().BeFalse();
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
            var options = new BoilerplateOptions { Items = ".gitignore,.editorconfig,global.json,nuget.config,Directory.Build.props" };

            BoilerplateTasks.Create(root, null, options);
            foreach (var (name, content) in files) File.ReadAllText(Path.Combine(root, name)).Should().Be(content);

            options.AppendExisting = true;
            BoilerplateTasks.Create(root, null, options);
            var gitignore = File.ReadAllText(Path.Combine(root, ".gitignore"));
            var editorconfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
            gitignore.Should().StartWith("my-ignore\n").And.Contain(".nuke/temp");
            editorconfig.Should().StartWith(files[".editorconfig"] + "\n").And.Contain("resharper_sort_usings");
            foreach (var name in new[] { "global.json", "NuGet.Config", "Directory.Build.props" })
                File.ReadAllText(Path.Combine(root, name)).Should().Be(files[name]);
            Directory.EnumerateFiles(root).Select(Path.GetFileName).Should().NotContain("nuget.config");

            BoilerplateTasks.Create(root, null, options);
            File.ReadAllText(Path.Combine(root, ".gitignore")).Should().Be(gitignore);
            File.ReadAllText(Path.Combine(root, ".editorconfig")).Should().Be(editorconfig);
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
                Items = "scripts/utils,.gitignore,Directory.Build.props",
                Exclude = "scripts/utils/git-hooks,.gitignore"
            };
            BoilerplateTasks.Create(root, null, options);
            File.Exists(Path.Combine(root, "scripts", "utils", "print-title.sh")).Should().BeTrue();
            Directory.Exists(Path.Combine(root, "scripts", "utils", "git-hooks")).Should().BeFalse();
            File.Exists(Path.Combine(root, ".gitignore")).Should().BeFalse();
            File.Exists(Path.Combine(root, "Directory.Build.props")).Should().BeTrue();
            File.Exists(Path.Combine(root, "Makefile")).Should().BeFalse();
            File.ReadAllText(Path.Combine(root, "existing.sln")).Should().Be("existing");
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
            var create = () => BoilerplateTasks.Create(root, null, new BoilerplateOptions { Items = ".gitignore,not-an-item" });
            create.Should().Throw<ArgumentException>().WithMessage("*not-an-item*");
            Directory.EnumerateFileSystemEntries(root).Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenSolutionOnly_AddsBuildWithoutCoreOrOtherFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "build"));
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            BoilerplateTasks.Create(root, "OnlyBuild", new BoilerplateOptions { Items = "solution" });
            File.ReadAllText(Path.Combine(root, "OnlyBuild.sln")).Should().Contain("_build.csproj").And.NotContain("Core.csproj");
            Directory.Exists(Path.Combine(root, "src")).Should().BeFalse();
            File.Exists(Path.Combine(root, "global.json")).Should().BeFalse();
            File.Exists(Path.Combine(root, "Directory.Build.props")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
