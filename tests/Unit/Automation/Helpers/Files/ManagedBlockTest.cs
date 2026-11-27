using Anarchy.Toolkit.Automation.Helpers.Files;
using AwesomeAssertions;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Helpers.Files;

public class ManagedBlockTest
{
    private const string Packaged = "# >>> anarchy-toolkit: managed >>>\n# Build\nbin/\nobj/\n# <<< anarchy-toolkit <<<\n";

    [Fact]
    public void MergeManagedBlock_GivenNoBlock_PlacesGitIgnoreBlockBeforeExistingOverrides()
    {
        var merged = FileHelper.MergeManagedBlock("bin/\nmine/\n", Packaged,
            skipExistingLines: true, placeBlockFirst: true);

        merged.Should().Be("# >>> anarchy-toolkit: managed >>>\n# Build\nobj/\n# <<< anarchy-toolkit <<<\n\nbin/\nmine/\n");
        FileHelper.MergeManagedBlock(merged, Packaged, skipExistingLines: true, placeBlockFirst: true).Should().BeSameAs(merged);
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

    [Fact]
    public void MergeManagedBlock_EditorConfigPlacesTheRootBlockBeforeProjectOverrides()
    {
        const string existing = "[*.cs]\nindent_size = 8\nroot = true\n";
        const string packaged = "# >>> anarchy-toolkit: managed >>>\nroot = true\n[*]\nindent_size = 4\n# <<< anarchy-toolkit <<<\n";

        var merged = FileHelper.MergeManagedBlock(existing, packaged, skipExistingLines: false, placeBlockFirst: true);

        merged.Should().StartWith("# >>> anarchy-toolkit: managed >>>\nroot = true\n[*]\nindent_size = 4\n# <<< anarchy-toolkit <<<\n\n[*.cs]")
            .And.Contain("[*.cs]\nindent_size = 8")
            .And.NotContain("root = true\n[*.cs]");
        merged.Split("root = true", StringSplitOptions.None).Should().HaveCount(2);
        FileHelper.MergeManagedBlock(merged, packaged, skipExistingLines: false, placeBlockFirst: true).Should().Be(merged);
    }

    [Fact]
    public void MergeManagedBlock_EditorConfigReplacesAndMovesAnExistingBlockBeforeOverrides()
    {
        const string existing = "[*]\nindent_size = 8\n\n# >>> anarchy-toolkit: old >>>\nroot = false\n[*]\nindent_size = 4\n# <<< anarchy-toolkit <<<\n";
        const string packaged = "# >>> anarchy-toolkit: managed >>>\nroot = true\n[*]\nindent_size = 2\n# <<< anarchy-toolkit <<<\n";

        var merged = FileHelper.MergeManagedBlock(existing, packaged, skipExistingLines: false, placeBlockFirst: true);

        merged.Should().StartWith("# >>> anarchy-toolkit: managed >>>\nroot = true\n[*]\nindent_size = 2\n# <<< anarchy-toolkit <<<\n\n[*]\nindent_size = 8\n");
        merged.Split(FileHelper.ManagedBlockBegin, StringSplitOptions.None).Should().HaveCount(2);
        merged.Should().NotContain("root = false");
    }

    [Fact]
    public void MergeManagedBlock_GitIgnoreRemovesAnEmptyHeadingAfterDroppingDuplicateRules()
    {
        const string existing = "bin/\n";
        const string packaged = "# >>> anarchy-toolkit: managed >>>\n# Duplicate output\nbin/\n# Build output\nobj/\n# <<< anarchy-toolkit <<<\n";

        var merged = FileHelper.MergeManagedBlock(existing, packaged, skipExistingLines: true, placeBlockFirst: true);

        merged.Should().Contain("bin/\n").And.NotContain("# Duplicate output").And.Contain("# Build output\nobj/");
        FileHelper.MergeManagedBlock(merged, packaged, skipExistingLines: true).Should().Be(merged);
    }

    [Fact]
    public void MergeManagedBlock_GitIgnoreKeepsProjectExceptionsAfterPackagedRules()
    {
        const string existing = "!important.txt\n";
        const string packaged = "# >>> anarchy-toolkit: managed >>>\n*.txt\n# <<< anarchy-toolkit <<<\n";

        var merged = FileHelper.MergeManagedBlock(existing, packaged, skipExistingLines: true, placeBlockFirst: true);

        merged.IndexOf("*.txt", StringComparison.Ordinal).Should().BeLessThan(merged.IndexOf("!important.txt", StringComparison.Ordinal));
        FileHelper.MergeManagedBlock(merged, packaged, skipExistingLines: true, placeBlockFirst: true).Should().Be(merged);
    }

    [Theory]
    [InlineData(".gitignore")]
    [InlineData(".editorconfig")]
    public void RepositorySettings_StartWithThePackagedBlock(string file)
    {
        var root = RepositoryRoot();
        var packaged = File.ReadAllText(Path.Combine(root, "src", "Automation", "Boilerplate", file)).ReplaceLineEndings("\n");

        packaged.Should().StartWith(FileHelper.ManagedBlockBegin).And.EndWith(FileHelper.ManagedBlockEnd + " <<<\n");
        File.ReadAllText(Path.Combine(root, file)).ReplaceLineEndings("\n").Should().StartWith(packaged);
        FileHelper.MergeManagedBlock(File.ReadAllText(Path.Combine(root, file)), packaged,
                skipExistingLines: file == ".gitignore", placeBlockFirst: file is ".gitignore" or ".editorconfig")
            .Should().Be(File.ReadAllText(Path.Combine(root, file)));
    }

    [Fact]
    public void PackagedGitignore_HasNoProjectSpecificEntries() =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Automation", "Boilerplate", ".gitignore"))
            .Should().NotContain("launchSettings").And.NotContain("cdk").And.NotContain("Lambda");

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "..", ".."));
}
