using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using AwesomeAssertions;
using System.Diagnostics;
using static Anarchy.Toolkit.Build.Nuke.Component.Tasks.ReleaseVersionTasks;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class ReleaseVersionTasksTest
{
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
}
