using Anarchy.Toolkit.Build.Cli.Core.Service;
using AwesomeAssertions;
using System.Diagnostics;
using static Anarchy.Toolkit.Build.Cli.Core.Service.ReleaseVersionTasks;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Core.Service;

public class ReleaseVersionTasksTest
{
    [Fact]
    public void FalloutConfigurationAndSharedBoilerplate_AreClassifiedCorrectly()
    {
        IsIgnoredFile(".fallout/parameters.json").Should().BeTrue();
        var project = new Project("/repo/src/Build.Cli/Build.Cli.csproj", "Build.Cli", "1.0.0", [], "Anarchy.Boilerplate/");
        var other = new Project("/repo/src/Core/Core.csproj", "Core", "1.0.0", [], "<Project />");
        DetectDirectlyAffectedProjects("/repo", [project, other], [".editorconfig"])
            .Should().ContainSingle().Which.Should().Be(project);
        DetectDirectlyAffectedProjects("/repo", [project, other], ["src/Build.Cli/Boilerplate/scripts/build.fallout.sh"])
            .Should().ContainSingle().Which.Should().Be(project);
        DetectDirectlyAffectedProjects("/repo", [project, other], ["Makefile"]).Should().BeEmpty();
    }

    private static Project Project(string name, string version, params string[] references) => new(
        $"/{name}/{name}.csproj", name, version, references.Select(r => $"/{r}/{r}.csproj").ToArray(),
        $"<Project>\n  <Version>{version}</Version>\n</Project>\n");

    [Theory]
    [InlineData(Bump.Patch, "2.3.5")]
    [InlineData(Bump.Minor, "2.4.0")]
    [InlineData(Bump.Major, "3.0.0")]
    public void Increment_UsesSemanticVersionResets(Bump bump, string expected) =>
        Increment("2.3.4", bump).Should().Be(expected);

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0-preview")]
    [InlineData("01.0.0")]
    [InlineData("$(Version)")]
    public void Increment_RejectsUnsupportedVersions(string version) =>
        ((Action)(() => Increment(version, Bump.Patch))).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Plan_BumpsDiamondAndTestingDependentsOnceAndLeavesOthersAlone()
    {
        var projects = new[] {
            Project("Core", "1.2.3"), Project("Core.Events", "2.4.5", "Core"),
            Project("Infrastructure", "3.1.9", "Core"), Project("Web", "4.6.1", "Core.Events", "Infrastructure"),
            Project("Testing", "1.0.0", "Web"), Project("Unrelated", "7.0.0")
        };

        var edits = Plan(projects, "Core", Bump.Minor);

        edits.Should().HaveCount(5);
        edits[projects[0].Path].Should().Be("<Project>\n  <Version>1.3.0</Version>\n</Project>\n");
        edits[projects[3].Path].Should().Contain("<Version>4.7.0</Version>");
        edits[projects[4].Path].Should().Contain("<Version>1.1.0</Version>");
        edits.Should().NotContainKey(projects[5].Path);
    }

    [Fact]
    public void Plan_UnknownOrAmbiguousProjectFails()
    {
        var projects = new[] { Project("Core", "1.0.0"), Project("Core", "1.0.0") };
        ((Action)(() => Plan(projects, "Missing", Bump.Patch))).Should().Throw<InvalidOperationException>();
        ((Action)(() => Plan(projects, "Core", Bump.Patch))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_ReportsMissingAndInsufficientTransitiveBumps()
    {
        var projects = new[] { Project("Core", "1.1.0"), Project("Infra", "1.0.1", "Core"), Project("Web", "1.0.0", "Infra") };
        var baseline = projects.ToDictionary(p => p.Path, _ => "1.0.0");

        var errors = Validate(projects, baseline);

        errors.Should().Contain(e => e.Contains("Infra: needs at least a minor bump") && e.Contains("Core"));
        errors.Should().Contain(e => e.Contains("Web: needs at least a minor bump") && e.Contains("Core"));
    }

    [Fact]
    public void Validate_AllowsStrongerBumpsAndNewProjectsButRejectsDecreases()
    {
        var projects = new[] { Project("Core", "1.0.1"), Project("Infra", "2.0.0", "Core"), Project("New", "1.0.0", "Infra") };
        var baseline = new Dictionary<string, string> { [projects[0].Path] = "1.0.0", [projects[1].Path] = "1.0.0" };
        Validate(projects, baseline).Should().BeEmpty();
        baseline[projects[0].Path] = "1.1.0";
        Validate(projects, baseline).Should().Contain(e => e.Contains("version decreased"));
    }

    [Fact]
    public void ReadProjects_ResolvesReferencesAndRejectsAmbiguousVersion()
    {
        var paths = new[] { Path.GetFullPath("Core/Core.csproj"), Path.GetFullPath("Web/Web.csproj") };
        var contents = new Dictionary<string, string> {
            [paths[0]] = "<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>",
            [paths[1]] = "<Project><Version>1.0.0</Version><ItemGroup><ProjectReference Include=\"..\\Core\\Core.csproj\" /></ItemGroup></Project>"
        };
        var projects = ReadProjects(paths, p => contents[p]);
        projects.Single(p => p.Name == "Web").References.Should().Equal(paths[0]);
        contents[paths[0]] = "<Project><Version>1.0.0</Version><Version>2.0.0</Version></Project>";
        ((Action)(() => ReadProjects(paths, p => contents[p]))).Should().Throw<InvalidOperationException>();
    }

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
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MultipleRootsAndCyclesAreBumpedOnce()
    {
        var projects = new[] { Project("A", "1.0.0", "C"), Project("B", "1.0.0", "A"),
            Project("C", "1.0.0", "B"), Project("D", "1.0.0") };
        var edits = Plan(projects, ["A", "B"], Bump.Patch);
        edits.Should().HaveCount(3);
        edits.Values.Should().OnlyContain(value => value.Contains("1.0.1"));
    }

    [Fact]
    public void ChangedFiles_MapProjectsAndSharedConfiguration()
    {
        var root = Path.GetFullPath("release-fixture");
        var a = new Project(Path.Combine(root, "src/A/A.csproj"), "A", "1.0.0", [], "<Project/>");
        var b = new Project(Path.Combine(root, "src/B/B.csproj"), "B", "1.0.0", [a.Path], "<Project/>");
        var c = new Project(Path.Combine(root, "src/C/C.csproj"), "C", "1.0.0", [], "<Project/>");
        var projects = new[] { a, b, c };
        DetectAffectedProjects(root, projects, ["src/A/README.md", "tests/A/Test.cs", "docs/example.md"])
            .Select(p => p.Name).Should().Equal("A", "B");
        DetectAffectedProjects(root, projects, ["tests/A/Test.cs", "docs/example.md", "README.md"])
            .Should().BeEmpty();
        DetectAffectedProjects(root, projects, ["src/Directory.Build.targets"]).Should().HaveCount(3);
        DetectAffectedProjects(root, projects, ["custom.targets"]).Should().HaveCount(3);
    }

    [Fact]
    public void CentralVersions_UseTransitiveAssetsAndConservativeFallbacks()
    {
        var root = Path.GetFullPath("release-fixture");
        var a = new Project(Path.Combine(root, "src/A/A.csproj"), "A", "1.0.0", [], "<Project/>");
        var b = new Project(Path.Combine(root, "src/B/B.csproj"), "B", "1.0.0", [a.Path], "<Project/>");
        var c = new Project(Path.Combine(root, "src/C/C.csproj"), "C", "1.0.0", [], "<Project/>");
        var projects = new[] { a, b, c };
        const string oldProps = "<Project><ItemGroup><PackageVersion Include='Indirect' Version='1.0.0'/><PackageVersion Include='Unused' Version='1.0.0'/></ItemGroup></Project>";
        const string newProps = "<Project><ItemGroup><PackageVersion Include='Indirect' Version='2.0.0'/><PackageVersion Include='Unused' Version='1.0.0'/></ItemGroup></Project>";
        string Assets(Project p) => p.Name == "A" ? "{\"libraries\":{\"Indirect/2.0.0\":{\"type\":\"package\"}}}" : "{\"libraries\":{}}";
        DetectAffectedProjects(root, projects, ["Directory.Packages.props"], _ => oldProps, _ => newProps, Assets)
            .Select(p => p.Name).Should().Equal("A", "B");
        DetectAffectedProjects(root, projects, ["Directory.Packages.props"], _ => oldProps,
            _ => newProps.Replace("Unused' Version='1.0.0", "Unused' Version='2.0.0").Replace("Indirect' Version='2.0.0", "Indirect' Version='1.0.0"), Assets)
            .Should().BeEmpty();
        DetectAffectedProjects(root, projects, ["Directory.Packages.props"], _ => oldProps,
            _ => "<Project><ItemGroup><PackageVersion Include='Unused' Version='1.0.0'/></ItemGroup></Project>", Assets)
            .Should().HaveCount(3);
        ((Action)(() => DetectAffectedProjects(root, projects, ["Directory.Packages.props"], _ => oldProps, _ => newProps, _ => null)))
            .Should().Throw<InvalidOperationException>();
        const string addedProps = "<Project><ItemGroup><PackageVersion Include='Indirect' Version='1.0.0'/><PackageVersion Include='Unused' Version='1.0.0'/><PackageVersion Include='New' Version='1.0.0'/></ItemGroup></Project>";
        DetectAffectedProjects(root, projects, ["Directory.Packages.props"], _ => oldProps, _ => addedProps,
            p => p.Name == "C" ? "{\"libraries\":{\"New/1.0.0\":{\"type\":\"package\"}}}" : Assets(p))
            .Select(p => p.Name).Should().Equal("C");
        DetectAffectedProjects(root, projects, ["Directory.Packages.props"], _ => oldProps,
            _ => newProps.Replace("<ItemGroup>", "<PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup><ItemGroup>"), Assets)
            .Should().HaveCount(3);
    }

    [Fact]
    public void AutomaticPlanPreservesManualVersionsAndRejectsDecreases()
    {
        var projects = new[] { Project("A", "1.1.0"), Project("B", "1.0.0", "A") };
        var baseline = projects.ToDictionary(p => p.Path, _ => "1.0.0");
        var edits = PlanChanges(projects, ["A"], Bump.Patch, baseline);
        edits.Should().ContainSingle().Which.Key.Should().Be(projects[1].Path);
        baseline[projects[0].Path] = "2.0.0";
        ((Action)(() => PlanChanges(projects, ["A"], Bump.Patch, baseline))).Should().Throw<InvalidOperationException>();
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
        finally { Directory.Delete(root, recursive: true); }
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
        finally { Directory.Delete(root, recursive: true); }
    }
}
