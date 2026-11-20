using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Automation.Infrastructure.Helper;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Service;

public class BoilerplateCatalogTest
{
    private static string TempRoot() => Path.Combine(Path.GetTempPath(), "anarchy-catalog-" + Guid.NewGuid());

    [Fact]
    public void ListAndRead_ExposeThePackagedFiles()
    {
        BoilerplateCatalog.List().Should().Contain(["Makefile", ".gitignore", ".editorconfig", "scripts/build.anarchy.sh", ".agents/skills/anarchy/SKILL.md"]);
        BoilerplateCatalog.List().Should().NotContain("scripts/build.fallout.sh");
        BoilerplateCatalog.Read("Makefile").Should().Contain("include");
        BoilerplateCatalog.Read(".\\nope").Should().BeNull();
    }

    [Fact]
    public void Compare_ReportsMissingIdenticalAndDifferentFiles()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root);
            SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = "Makefile,scripts/console" });
            File.AppendAllText(Path.Combine(root, "scripts", "console", "title.sh"), "echo mine\n");
            File.WriteAllText(Path.Combine(root, ".gitignore"), "mine/\n");

            var result = BoilerplateCatalog.Compare(root, ["Makefile", "scripts/console", ".gitignore", ".editorconfig"]);

            result.Single(file => file.Path == "Makefile").Status.Should().Be(BoilerplateCatalog.FileStatus.Identical);
            result.Single(file => file.Path == ".editorconfig").Status.Should().Be(BoilerplateCatalog.FileStatus.Missing);
            result.Single(file => file.Path == "scripts/console/title.sh").Diff.Should().Contain("-echo mine");
            result.Single(file => file.Path == ".gitignore").Diff.Should().Contain(" mine/").And.Contain("+# >>> anarchy-toolkit");

            File.WriteAllText(Path.Combine(root, ".gitignore"), FileMerge(root));
            BoilerplateCatalog.Compare(root, [".gitignore"]).Single().Status.Should().Be(BoilerplateCatalog.FileStatus.Identical);

            var editorConfigPath = Path.Combine(root, ".editorconfig");
            File.WriteAllText(editorConfigPath, "[*]\nindent_size = 8\n");
            var editorConfig = BoilerplateCatalog.Compare(root, [".editorconfig"]).Should().ContainSingle().Which;
            editorConfig.Status.Should().Be(BoilerplateCatalog.FileStatus.Different);
            editorConfig.Diff.Should().Contain("+root = true");
            editorConfig.Diff.IndexOf("+# >>> anarchy-toolkit", StringComparison.Ordinal)
                .Should().BeLessThan(editorConfig.Diff.IndexOf(" [*]", StringComparison.Ordinal));

            var packagedEditorConfig = BoilerplateCatalog.Read(".editorconfig")!;
            File.WriteAllText(editorConfigPath, FileHelper.MergeManagedBlock(
                File.ReadAllText(editorConfigPath), packagedEditorConfig, skipExistingLines: false, placeBlockFirst: true));
            BoilerplateCatalog.Compare(root, [".editorconfig"]).Single().Status.Should().Be(BoilerplateCatalog.FileStatus.Identical);

            var unknown = () => BoilerplateCatalog.Compare(root, ["nope"]);
            unknown.Should().Throw<ArgumentException>().WithMessage("*nope*");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenDryRun_ReportsWithoutWriting()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "Makefile"), "mine");
            File.WriteAllText(Path.Combine(root, ".gitignore"), "mine/\n");
            var messages = new List<string>();

            SolutionScaffolder.Create(root, null, new ScaffoldOptions {
                Items = "Makefile,scripts/console,.gitignore",
                AppendExisting = true,
                DryRun = true,
                Log = messages.Add
            });

            messages.Should().Contain(message => message.StartsWith("Conflict: Makefile"))
                .And.Contain("Create: scripts/console/title.sh")
                .And.Contain(message => message.StartsWith("Update the managed block of .gitignore") && message.Contains("+.fallout/temp"));
            Directory.EnumerateFileSystemEntries(root).Select(Path.GetFileName).Should().BeEquivalentTo("Makefile", ".gitignore");
            File.ReadAllText(Path.Combine(root, ".gitignore")).Should().Be("mine/\n");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void UnifiedDiff_GroupsChangesIntoHunksWithContext()
    {
        var before = string.Join('\n', Enumerable.Range(1, 20)) + "\n";
        var after = string.Join('\n', Enumerable.Range(1, 20).Where(line => line != 19).Select(line => line == 2 ? "two" : line.ToString())) + "\n";

        UnifiedDiff.Create("f", before, after).Should().Be(
            "--- a/f\n+++ b/f\n" +
            "@@ -1,5 +1,5 @@\n 1\n-2\n+two\n 3\n 4\n 5\n" +
            "@@ -16,5 +16,4 @@\n 16\n 17\n 18\n-19\n 20\n");
        UnifiedDiff.Create("f", before, before.ReplaceLineEndings("\r\n")).Should().BeEmpty();
    }

    private static string FileMerge(string root) => Anarchy.Toolkit.Automation.Infrastructure.Helper.FileHelper.MergeManagedBlock(
        File.ReadAllText(Path.Combine(root, ".gitignore")), BoilerplateCatalog.Read(".gitignore")!,
        skipExistingLines: true, placeBlockFirst: true);
}
