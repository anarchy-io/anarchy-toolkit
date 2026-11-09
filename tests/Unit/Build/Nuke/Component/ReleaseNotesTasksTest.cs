using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using AwesomeAssertions;
using System.Diagnostics;
using static Anarchy.Toolkit.Build.Nuke.Component.Tasks.ReleaseVersionTasks;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class ReleaseNotesTasksTest
{
    [Fact]
    public void AddEntry_InsertsBeforeOldReleasesAndPreservesExistingVersion()
    {
        const string readme = "# Package\n\n## Releases\n\n### 1.0.0\n\n- Old note\n\n## Other\n";
        var updated = ReleaseNotesTasks.AddEntry(readme, "1.0.1", ["feat: add feature", "fix: repair", "fix: repair"]);
        updated.Should().Be("# Package\n\n## Releases\n\n### 1.0.1\n\n- feat: add feature\n- fix: repair\n\n### 1.0.0\n\n- Old note\n\n## Other\n");
        ReleaseNotesTasks.AddEntry(updated, "1.0.1", ["replacement"]).Should().Be(updated);
    }

    [Fact]
    public void AddEntry_HandlesMissingSectionWindowsLineEndingsAndMarkdownFences()
    {
        const string readme = "# Package\r\n\r\n```md\r\n## Releases\r\n### 1.0.1\r\n```\r\n\r\n## Releases\r\n\r\n### 1.0.0\r\n\r\n- Old\r\n";
        var updated = ReleaseNotesTasks.AddEntry(readme, "1.0.1", ["fix: problem"]);
        updated.Should().Contain("## Releases\r\n\r\n### 1.0.1\r\n\r\n- fix: problem\r\n\r\n### 1.0.0");
        ReleaseNotesTasks.AddEntry("# Package", "2.0.0", ["feat: new"])
            .Should().Be("# Package\n\n## Releases\n\n### 2.0.0\n\n- feat: new\n");
        ReleaseNotesTasks.AddEntry("## Releases\n\n### [v1.0.1] - date\n\nCustom text\n", "1.0.1", ["ignored"])
            .Should().Contain("Custom text").And.NotContain("ignored");
    }

    [Fact]
    public void AddEntry_RejectsMissingMessagesAndDoesNotMatchVersionPrefixes()
    {
        ((Action)(() => ReleaseNotesTasks.AddEntry("## Releases\n", "1.0.1", []))).Should().Throw<InvalidOperationException>();
        ReleaseNotesTasks.AddEntry("## Releases\n\n### 1.0.10\n", "1.0.1", ["fix"])
            .Should().Contain("### 1.0.1\n").And.Contain("### 1.0.10\n");
    }

    [Fact]
    public void Collect_AttributesCommitSubjectsToChangedProjectsAndDependents()
    {
        var root = Path.Combine(Path.GetTempPath(), "release-notes-test-" + Guid.NewGuid().ToString("N"));
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
            void Commit(string message)
            {
                Git("add", ".");
                Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-qm", message);
            }
            Git("init", "-q");
            foreach (var name in new[] { "A", "B", "C" }) Directory.CreateDirectory(Path.Combine(root, "src", name));
            var a = Path.Combine(root, "src/A/A.csproj");
            var b = Path.Combine(root, "src/B/B.csproj");
            var c = Path.Combine(root, "src/C/C.csproj");
            File.WriteAllText(a, "<Project><Version>1.0.1</Version></Project>");
            File.WriteAllText(b, "<Project><Version>1.0.1</Version><ProjectReference Include='../A/A.csproj'/></Project>");
            File.WriteAllText(c, "<Project><Version>1.0.1</Version></Project>");
            Commit("baseline");
            File.WriteAllText(Path.Combine(root, "src/A/Source.cs"), "// A");
            Commit("feat: A functionality");
            File.WriteAllText(Path.Combine(root, "src/C/Source.cs"), "// C");
            Commit("fix: C functionality");
            Directory.CreateDirectory(Path.Combine(root, "tests"));
            File.WriteAllText(Path.Combine(root, "tests/Test.cs"), "// test");
            Commit("test: unrelated");
            var projects = ReadProjects([a, b, c], File.ReadAllText);
            var subjects = ReleaseNotesTasks.Collect(root, projects, "HEAD~3", "HEAD");
            subjects[a].Should().Equal("feat: A functionality");
            subjects[b].Should().Equal("feat: A functionality");
            subjects[c].Should().Equal("fix: C functionality");
            var edits = ReleaseNotesTasks.Plan(projects, subjects, path => path == Path.Combine(root, "src/B/README.md")
                ? "## Releases\n\n### 1.0.1\n\n- Hand-written\n" : "## Releases\n\n### 1.0.0\n");
            edits.Should().HaveCount(2);
            edits[Path.Combine(root, "src/A/README.md")].Should().Contain("- feat: A functionality").And.NotContain("fix: C");
            edits[Path.Combine(root, "src/C/README.md")].Should().Contain("- fix: C functionality");
            ((Action)(() => ReleaseNotesTasks.Collect(root, projects, "HEAD", "HEAD~3"))).Should().Throw<InvalidOperationException>();
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Collect_IncludesMergedBranchCommitsAndMergeSubject()
    {
        var root = Path.Combine(Path.GetTempPath(), "release-notes-merge-" + Guid.NewGuid().ToString("N"));
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
            Git("switch", "-qc", "release-test-main");
            Git("config", "user.name", "Test");
            Git("config", "user.email", "test@example.com");
            Directory.CreateDirectory(Path.Combine(root, "src/A"));
            var projectPath = Path.Combine(root, "src/A/A.csproj");
            File.WriteAllText(projectPath, "<Project><Version>1.0.1</Version></Project>");
            Git("add", ".");
            Git("commit", "-qm", "base");
            Git("switch", "-qc", "release-test-feature");
            File.WriteAllText(Path.Combine(root, "src/A/Feature.cs"), "// feature");
            Git("add", ".");
            Git("commit", "-qm", "feat: add feature");
            Git("switch", "-q", "release-test-main");
            Git("merge", "--no-ff", "-qm", "merge feature", "release-test-feature");
            var projects = ReadProjects([projectPath], File.ReadAllText);
            var subjects = ReleaseNotesTasks.Collect(root, projects, "HEAD^1", "HEAD");
            subjects[projectPath].Should().Equal("feat: add feature", "merge feature");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Collect_AttributesCentralDependencyUpdatesToConsumers()
    {
        var root = Path.Combine(Path.GetTempPath(), "release-notes-central-" + Guid.NewGuid().ToString("N"));
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
            Git("config", "user.name", "Test");
            Git("config", "user.email", "test@example.com");
            foreach (var name in new[] { "A", "B" }) Directory.CreateDirectory(Path.Combine(root, "src", name));
            var a = Path.Combine(root, "src/A/A.csproj");
            var b = Path.Combine(root, "src/B/B.csproj");
            File.WriteAllText(a, "<Project><Version>1.0.1</Version></Project>");
            File.WriteAllText(b, "<Project><Version>1.0.1</Version></Project>");
            var props = Path.Combine(root, "Directory.Packages.props");
            File.WriteAllText(props, "<Project><ItemGroup><PackageVersion Include='Indirect' Version='1.0.0'/></ItemGroup></Project>");
            Git("add", ".");
            Git("commit", "-qm", "base");
            File.WriteAllText(props, "<Project><ItemGroup><PackageVersion Include='Indirect' Version='2.0.0'/></ItemGroup></Project>");
            Git("add", ".");
            Git("commit", "-qm", "build: update dependency");
            foreach (var name in new[] { "A", "B" }) Directory.CreateDirectory(Path.Combine(root, "src", name, "obj"));
            File.WriteAllText(Path.Combine(root, "src/A/obj/project.assets.json"), "{\"libraries\":{\"Indirect/2.0.0\":{\"type\":\"package\"}}}");
            File.WriteAllText(Path.Combine(root, "src/B/obj/project.assets.json"), "{\"libraries\":{}}");
            var projects = ReadProjects([a, b], File.ReadAllText);
            var subjects = ReleaseNotesTasks.Collect(root, projects, "HEAD~1", "HEAD");
            subjects.Should().ContainSingle();
            subjects[a].Should().Equal("build: update dependency");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
