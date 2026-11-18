using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;
using AwesomeAssertions;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Infrastructure.Helper;

public class ManagedBlockTest
{
    private const string Packaged = "# >>> anarchy-toolkit: managed >>>\n# Build\nbin/\nobj/\n# <<< anarchy-toolkit <<<\n";

    [Fact]
    public void MergeManagedBlock_GivenNoBlock_AppendsOneWithoutTheLinesTheFileHas()
    {
        var merged = FileHelper.MergeManagedBlock("bin/\nmine/\n", Packaged, skipExistingLines: true);

        merged.Should().Be("bin/\nmine/\n\n# >>> anarchy-toolkit: managed >>>\n# Build\nobj/\n# <<< anarchy-toolkit <<<\n");
        FileHelper.MergeManagedBlock(merged, Packaged, skipExistingLines: true).Should().BeSameAs(merged);
    }

    [Fact]
    public void MergeManagedBlock_GivenOutdatedBlock_ReplacesOnlyTheBlock()
    {
        const string existing = "top\n# >>> anarchy-toolkit: old >>>\nold/\n# <<< anarchy-toolkit <<<\n\n# Project\nmine/\n";

        FileHelper.MergeManagedBlock(existing, Packaged, skipExistingLines: true)
            .Should().Be("top\n# >>> anarchy-toolkit: managed >>>\n# Build\nbin/\nobj/\n# <<< anarchy-toolkit <<<\n\n# Project\nmine/\n");
    }

    [Fact]
    public void MergeManagedBlock_GivenUnmarkedPackagedText_WrapsItAndKeepsRepeatedLines()
    {
        var merged = FileHelper.MergeManagedBlock("[*]\nindent_size = 8\r\n", "[*]\nindent_size = 4\n", skipExistingLines: false);

        merged.Should().StartWith("[*]\r\nindent_size = 8\r\n\r\n# >>> anarchy-toolkit")
            .And.EndWith("[*]\r\nindent_size = 4\r\n# <<< anarchy-toolkit <<<\r\n");
    }

    [Theory]
    [InlineData(".gitignore")]
    [InlineData(".editorconfig")]
    public void RepositorySettings_StartWithThePackagedBlock(string file)
    {
        var root = RepositoryRoot();
        var packaged = File.ReadAllText(Path.Combine(root, "src", "Build.Cli", "Boilerplate", file)).ReplaceLineEndings("\n");

        packaged.Should().StartWith(FileHelper.ManagedBlockBegin).And.EndWith(FileHelper.ManagedBlockEnd + " <<<\n");
        File.ReadAllText(Path.Combine(root, file)).ReplaceLineEndings("\n").Should().StartWith(packaged);
        FileHelper.MergeManagedBlock(File.ReadAllText(Path.Combine(root, file)), packaged, file == ".gitignore")
            .Should().Be(File.ReadAllText(Path.Combine(root, file)));
    }

    [Fact]
    public void PackagedGitignore_HasNoProjectSpecificEntries() =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Build.Cli", "Boilerplate", ".gitignore"))
            .Should().NotContain("launchSettings").And.NotContain("cdk").And.NotContain("Lambda");

    [Fact]
    public void Create_GivenTrackedFileThatTheNewGitignoreIgnores_WarnsAboutIt()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-gitignore-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            File.WriteAllText(Path.Combine(root, "src", "appsettings.Development.json"), "{}");
            if (LocalProcessTasks.Run(root, "git", ["init", "-q"]).ExitCode != 0) return;
            LocalProcessTasks.Require(root, "git", "add", "src/appsettings.Development.json");

            var messages = new List<string>();
            SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = ".gitignore", Log = messages.Add });

            messages.Should().ContainSingle(message => message.Contains("'src/appsettings.Development.json' is tracked"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "..", "..", ".."));
}
