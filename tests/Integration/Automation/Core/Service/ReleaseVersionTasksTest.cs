using AwesomeAssertions;
using System.Diagnostics;
using static Anarchy.Toolkit.Automation.Core.Service.ReleaseVersionTasks;

namespace Anarchy.Toolkit.Tests.Integration.Automation.Core.Service;

public class ReleaseVersionTasksTest
{
    [Fact]
    public void ReadBaseline_UsesCommittedVersionsAndHandlesNewProjectsAndInvalidRefs()
    {
        var root = Path.Combine(Path.GetTempPath(), "release-version-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            void Git(params string[] args)
            {
                var start = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var arg in args) start.ArgumentList.Add(arg);
                using var process = Process.Start(start)!;
                process.WaitForExit();
                process.ExitCode.Should().Be(0, process.StandardError.ReadToEnd());
            }
            Git("init", "-q");
            var oldPath = Path.Combine(root, "Core.csproj");
            var newPath = Path.Combine(root, "New.csproj");
            File.WriteAllText(oldPath, "<Project><Version>1.0.0</Version></Project>");
            Git("add", "Core.csproj");
            Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-qm", "initial");
            File.WriteAllText(oldPath, "<Project><Version>1.1.0</Version></Project>");
            File.WriteAllText(newPath, "<Project><Version>1.0.0</Version></Project>");

            var baseline = ReadBaseline(root, [oldPath, newPath], "HEAD");
            baseline.Should().ContainSingle().Which.Value.Should().Be("1.0.0");
            ((Action)(() => ReadBaseline(root, [oldPath], "not-a-real-ref"))).Should().Throw<InvalidOperationException>();
        }
        finally
        {
            TestDirectory.Delete(root);
        }
    }

    [Fact]
    public void GitDiff_HandlesRenamesAndUnusualPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "release-diff-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            void Git(params string[] args)
            {
                var start = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var arg in args) start.ArgumentList.Add(arg);
                using var process = Process.Start(start)!;
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                process.ExitCode.Should().Be(0, output + error);
            }
            Git("init", "-q");
            Directory.CreateDirectory(Path.Combine(root, "src/A"));
            File.WriteAllText(Path.Combine(root, "src/A/old file.cs"), "content");
            Git("add", ".");
            Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-qm", "base");
            Git("mv", "src/A/old file.cs", "src/A/new file.cs");
            Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-qm", "head");
            GetChangedFiles(root, "HEAD~1", "HEAD").Should().BeEquivalentTo(["src/A/old file.cs", "src/A/new file.cs"]);
            ((Action)(() => GetChangedFiles(root, "0000000000000000000000000000000000000000", "HEAD"))).Should().Throw<InvalidOperationException>();
            VerifyWorkingTree(root, "HEAD");
            File.WriteAllText(Path.Combine(root, "src/A/new file.cs"), "dirty");
            ((Action)(() => VerifyWorkingTree(root, "HEAD"))).Should().Throw<InvalidOperationException>();
        }
        finally { TestDirectory.Delete(root); }
    }

    [Fact]
    public void GitReleasePlan_BumpsOnlyChangedProjectsAndDependents()
    {
        var root = Path.Combine(Path.GetTempPath(), "release-integration-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            void Git(params string[] args)
            {
                var start = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var arg in args) start.ArgumentList.Add(arg);
                using var process = Process.Start(start)!;
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                process.ExitCode.Should().Be(0, output + error);
            }
            Git("init", "-q");
            foreach (var name in new[] { "A", "B", "C" }) Directory.CreateDirectory(Path.Combine(root, "src", name));
            var a = Path.Combine(root, "src/A/A.csproj");
            var b = Path.Combine(root, "src/B/B.csproj");
            var c = Path.Combine(root, "src/C/C.csproj");
            File.WriteAllText(a, "<Project><Version>1.0.0</Version></Project>");
            File.WriteAllText(b, "<Project><Version>2.0.0</Version><ProjectReference Include='../A/A.csproj'/></Project>");
            File.WriteAllText(c, "<Project><Version>3.0.0</Version></Project>");
            Git("add", ".");
            Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-qm", "base");
            File.WriteAllText(Path.Combine(root, "src/A/Source.cs"), "// changed");
            Git("add", ".");
            Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-qm", "head");
            var projects = ReadProjects([a, b, c], File.ReadAllText);
            var affected = DetectAffectedProjects(root, projects, "HEAD~1", "HEAD");
            var baseline = ReadBaseline(root, [a, b, c], "HEAD~1");
            var edits = PlanChanges(projects, affected.Select(p => p.Name), Bump.Patch, baseline);
            foreach (var (path, content) in edits) File.WriteAllText(path, content);
            File.ReadAllText(a).Should().Contain("1.0.1");
            File.ReadAllText(b).Should().Contain("2.0.1");
            File.ReadAllText(c).Should().Contain("3.0.0");
        }
        finally { TestDirectory.Delete(root); }
    }
}
