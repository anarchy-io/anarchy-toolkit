using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Build.Fallout.Component;

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
            .And.Contain("outdated test.sln --upgrade --no-restore --version-lock Minor")
            .And.Contain("dotnet restore test.sln");
        File.WriteAllText(log, "");
        DependencyUpgradeTasks.Run(repo.Root, "test.sln", "Major", false, false, false, dotnet, repo.Root);
        File.ReadAllText(log).Should().NotContain("--upgrade");
    }

    [Fact]
    public void DependencyUpgrade_UpdatesCentralTransitivePinsWithoutTouchingRangesOrNestedProjects()
    {
        using var repo = new TestRepository();
        var props = Path.Combine(repo.Root, "Directory.Packages.props");
        repo.Write("Directory.Packages.props", """
            <Project><ItemGroup>
              <PackageVersion Include="Core" Version="1.0.0" />
              <PackageVersion Include="Pinned" Version="[1.0.0]" />
              <PackageVersion Include="Conditional" Version="1.0.0" Condition="'a' == 'b'" />
              <PackageVersion Include="NestedOnly" Version="1.0.0" />
            </ItemGroup></Project>
            """);
        var nested = Path.Combine(repo.Root, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "Directory.Packages.props"), "<Project />");
        var report = Path.Combine(repo.Root, "report.json");
        var dependencies = new[] { "Core", "Pinned", "Conditional", "NestedOnly" }
            .Select(name => new { Name = name, ResolvedVersion = "1.0.0", LatestVersion = "1.1.0" }).ToArray();
        File.WriteAllText(report, System.Text.Json.JsonSerializer.Serialize(new {
            Projects = new[] {
            new { FilePath = Path.Combine(repo.Root, "test.csproj"), TargetFrameworks = new[] { new { Dependencies = dependencies[..3] } } },
            new { FilePath = Path.Combine(nested, "test.csproj"), TargetFrameworks = new[] { new { Dependencies = dependencies[3..] } } }
        }
        }));

        DependencyUpgradeTasks.UpdateCentralVersions(props, report);

        var result = File.ReadAllText(props);
        result.Should().Contain("Include=\"Core\" Version=\"1.1.0\"")
            .And.Contain("Include=\"Pinned\" Version=\"[1.0.0]\"")
            .And.Contain("Include=\"Conditional\" Version=\"1.0.0\"")
            .And.Contain("Include=\"NestedOnly\" Version=\"1.0.0\"")
            .And.NotContain("<?xml");
        DependencyUpgradeTasks.UpdateCentralVersions(props, report);
        File.ReadAllText(props).Should().Be(result);
    }

    [Fact]
    public void DependencyUpgrade_UpdatesPinsBeforeUpgradeAndFailsWhenRestoreFails()
    {
        if (OperatingSystem.IsWindows()) return;
        using var repo = new TestRepository();
        static string Quote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
        var dotnet = Path.Combine(repo.Root, "fake-dotnet");
        var tool = Path.Combine(repo.Root, ".dotnet", "tools", "dotnet-outdated");
        var props = Path.Combine(repo.Root, "Directory.Packages.props");
        var report = Path.Combine(repo.Root, "report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(tool)!);
        repo.Write("Directory.Packages.props", "<Project><ItemGroup><PackageVersion Include=\"Core\" Version=\"1.0.0\" /></ItemGroup></Project>");
        File.WriteAllText(report, System.Text.Json.JsonSerializer.Serialize(new {
            Projects = new[] {
            new { FilePath = Path.Combine(repo.Root, "test.csproj"), TargetFrameworks = new[] { new { Dependencies = new[] {
                new { Name = "Core", ResolvedVersion = "1.0.0", LatestVersion = "1.1.0" }
            } } } }
        }
        }));
        File.WriteAllText(dotnet, "#!/bin/bash\nif [ \"$1\" = restore ]; then exit 7; fi\n");
        File.WriteAllText(tool, "#!/bin/bash\n" +
            $"if [[ \" $* \" == *' --upgrade '* ]]; then grep -q 'Version=\"1.1.0\"' {Quote(props)} || exit 9; fi\n" +
            $"while [ \"$#\" -gt 0 ]; do if [ \"$1\" = --output ]; then cp {Quote(report)} \"$2\"; exit; fi; shift; done\n");
        File.SetUnixFileMode(dotnet, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.SetUnixFileMode(tool, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        ((Action)(() => DependencyUpgradeTasks.Run(repo.Root, "test.csproj", "Major", false, true, false, dotnet, repo.Root)))
            .Should().Throw<InvalidOperationException>().WithMessage("*exited with code 7*");
        File.ReadAllText(props).Should().Contain("Version=\"1.1.0\"");
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
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "fallout-local-test-" + Guid.NewGuid().ToString("N"));
        public TestRepository() { Directory.CreateDirectory(Root); Git("init", "-q"); }
        public void Write(string path, string value) => File.WriteAllText(Path.Combine(Root, path), value);
        public string Git(params string[] args) => LocalProcessTasks.Require(Root, "git", args);
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
