using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class LocalAutomationTasksTest
{
    [Fact]
    public void LintStaging_StagesOnlyChangesToPreviouslyStagedFilesWithoutUnstagedEdits()
    {
        using var repo = new TestRepository();
        repo.Write("safe file.cs", "old");
        repo.Write("partial.cs", "old");
        repo.Write("unstaged.cs", "old");
        repo.Git("add", ".");
        repo.Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-qm", "initial");
        repo.Write("safe file.cs", "staged");
        repo.Write("partial.cs", "staged");
        repo.Git("add", "safe file.cs", "partial.cs");
        repo.Write("partial.cs", "unstaged user edits");
        repo.Write("unstaged.cs", "unstaged user edits");

        LintStagingTasks.Run(repo.Root, true, () => {
            repo.Write("safe file.cs", "staged and formatted");
            repo.Write("partial.cs", "unstaged user edits and formatted");
            repo.Write("unstaged.cs", "unstaged user edits and formatted");
        });

        repo.Git("show", ":safe file.cs").Should().Be("staged and formatted");
        repo.Git("show", ":partial.cs").Should().Be("staged");
        repo.Git("show", ":unstaged.cs").Should().Be("old");
    }

    [Fact]
    public void LintStaging_DoesNotStageWhenDisabledOrWhenIndexChangesDuringLint()
    {
        using var repo = new TestRepository();
        repo.Write("a", "original");
        repo.Git("add", "a");
        LintStagingTasks.Run(repo.Root, false, () => repo.Write("a", "formatted"));
        repo.Git("show", ":a").Should().Be("original");
        repo.Git("add", "a");
        LintStagingTasks.Run(repo.Root, true, () => {
            repo.Write("a", "concurrent index update");
            repo.Git("add", "a");
            repo.Write("a", "later format");
        });
        repo.Git("show", ":a").Should().Be("concurrent index update");
    }

    [Fact]
    public void GitHooks_UseConfiguredDirectoryAndProtectExistingHooks()
    {
        using var repo = new TestRepository();
        repo.Git("config", "core.hooksPath", "custom hooks");
        var path = Path.Combine(repo.Root, "custom hooks", "pre-commit");
        GitHookTasks.Enable(repo.Root, "pre-commit", true, false, false, false, true);
        File.Exists(path).Should().BeFalse();
        GitHookTasks.Enable(repo.Root, "pre-commit", false, false, false, false, true);
        File.ReadAllText(path).Should().Contain("lint_fix AUTO_STAGE=true lint test_unit").And.NotContain("test_integration");
        GitHookTasks.Show(repo.Root, null).Should().Contain("pre-commit");
        ((Action)(() => GitHookTasks.Enable(repo.Root, "pre-commit", false, false, false, false, false))).Should().Throw<InvalidOperationException>();
        GitHookTasks.Enable(repo.Root, "pre-commit", false, true, true, true, false);
        File.ReadAllText(path).Should().Contain("test_integration test_end_to_end").And.NotContain("lint_fix");
        ((Action)(() => GitHookTasks.Disable(repo.Root, "../other"))).Should().Throw<ArgumentException>();
        GitHookTasks.Disable(repo.Root, "pre-commit");
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void GitHooks_AllSkippedDoesNotCreateHook()
    {
        using var repo = new TestRepository();
        GitHookTasks.Enable(repo.Root, "pre-commit", false, false, true, true, true);
        GitHookTasks.Show(repo.Root, "pre-commit").Should().Contain("No hook found");
    }

    [Fact]
    public void DependencyUpgrade_RejectsInvalidVersionLockBeforeStartingTools()
    {
        ((Action)(() => DependencyUpgradeTasks.Run(".", "test.sln", "invalid", false, false, false)))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DependencyUpgrade_RequiresExplicitPermissionWhenNoninteractive()
    {
        if (OperatingSystem.IsWindows()) return;
        using var repo = new TestRepository();
        var log = Path.Combine(repo.Root, "calls");
        var dotnet = Path.Combine(repo.Root, "fake-dotnet");
        var tool = Path.Combine(repo.Root, ".dotnet", "tools", "dotnet-outdated");
        Directory.CreateDirectory(Path.GetDirectoryName(tool)!);
        static string Quote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
        File.WriteAllText(dotnet, "#!/bin/bash\n" +
            $"printf 'dotnet %s\\n' \"$*\" >> {Quote(log)}\n" +
            $"if [ \"$1 $2\" = 'tool install' ]; then cp {Quote(dotnet + "-tool")} {Quote(tool)}; chmod +x {Quote(tool)}; fi\n");
        File.WriteAllText(dotnet + "-tool", "#!/bin/bash\n" + $"printf 'outdated %s\\n' \"$*\" >> {Quote(log)}\n");
        File.SetUnixFileMode(dotnet, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        DependencyUpgradeTasks.Run(repo.Root, "test.sln", "Major", false, false, false, dotnet, repo.Root);
        File.ReadAllText(log).Should().NotContain("tool install");
        DependencyUpgradeTasks.Run(repo.Root, "test.sln", "Minor", true, true, false, dotnet, repo.Root);
        File.ReadAllText(log).Should().Contain("tool install --global dotnet-outdated-tool")
            .And.Contain("outdated test.sln --upgrade --version-lock Minor");
        File.WriteAllText(log, "");
        DependencyUpgradeTasks.Run(repo.Root, "test.sln", "Major", false, false, false, dotnet, repo.Root);
        File.ReadAllText(log).Should().NotContain("--upgrade");
    }

    [Fact]
    public void LintStaging_TreatsGitPathspecCharactersLiterally()
    {
        using var repo = new TestRepository();
        repo.Write("special[1].cs", "old");
        repo.Write("special1.cs", "old");
        repo.Git("add", ".");
        repo.Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-qm", "initial");
        repo.Write("special[1].cs", "staged");
        repo.Git("add", "--", ":(literal)special[1].cs");
        repo.Write("special1.cs", "unrelated");
        LintStagingTasks.Run(repo.Root, true, () => repo.Write("special[1].cs", "formatted"));
        repo.Git("show", ":special[1].cs").Should().Be("formatted");
        repo.Git("show", ":special1.cs").Should().Be("old");
    }

    private sealed class TestRepository : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "nuke-local-test-" + Guid.NewGuid().ToString("N"));
        public TestRepository() { Directory.CreateDirectory(Root); Git("init", "-q"); }
        public void Write(string path, string value) => File.WriteAllText(Path.Combine(Root, path), value);
        public string Git(params string[] args) => LocalProcessTasks.Require(Root, "git", args);
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
