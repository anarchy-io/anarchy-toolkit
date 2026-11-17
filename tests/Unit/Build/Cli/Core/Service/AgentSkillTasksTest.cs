using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Core.Service;

public class AgentSkillTasksTest
{
    private static string TempRoot() => Path.Combine(Path.GetTempPath(), "anarchy-skills-" + Guid.NewGuid());

    [Fact]
    public void PackagedSkills_AreOptInAndCoverEveryPackagedMakeTarget()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root);
            SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = "Makefile,scripts" });
            Directory.Exists(Path.Combine(root, ".agents")).Should().BeFalse();
            SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = "skills" });

            var actions = AgentSkillTasks.ReadMakeTargets(root, "shop");
            var directories = new[] { Path.Combine(root, AgentSkillTasks.DefaultDirectory) };

            actions.Should().NotBeEmpty().And.OnlyContain(action => action.Skill != null && action.Skill.StartsWith("anarchy-"));
            AgentSkillTasks.Validate(directories, actions).Should().BeEmpty();
            AgentSkillTasks.Sync(directories, actions).Should().BeEmpty();
            AgentSkillTasks.ReadSkills(directories, []).Select(skill => skill.Name).Should().BeEquivalentTo(
                "anarchy", "anarchy-develop", "anarchy-environment", "anarchy-release", "anarchy-maintenance");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ParseMakeTargets_ReadsTargetsAndTheirSkillMarkers()
    {
        var actions = AgentSkillTasks.ParseMakeTargets("""
            ## HEADER
            #@ shop-tools
            VARIABLE := value
            first second: deps #? Does things
            #+ NAME|str|The name
            #! Destroys data
            	@echo first
            hidden: #
            own: #? Its own skill
            #@ shop-other
            exempt: #? No skill
            #@ -
            bare: #? Default name
            #@
            """, "my-tools", "My.Shop");

        actions.Select(action => (action.Name, action.Skill)).Should().Equal(
            ("first", "shop-tools"), ("own", "shop-other"), ("exempt", null), ("bare", "my-shop-my-tools"));
        actions[0].Should().BeEquivalentTo(new AgentAction("first", "Does things", "my-tools", "shop-tools") {
            Aliases = ["second"],
            Parameters = [new AgentActionParameter("NAME", "str", "The name")],
            Warnings = ["Destroys data"]
        });
        AgentSkillTasks.ParseMakeTargets("plain: #? No markers\n", "x", "shop").Single().Skill.Should().BeNull();
    }

    [Theory]
    [InlineData("Anarchy.Toolkit", "anarchy-toolkit")]
    [InlineData("mock-server", "mock-server")]
    [InlineData("check_layering", "check-layering")]
    [InlineData("--", "")]
    public void ToSkillName_JoinsLowercaseWordsWithHyphens(string value, string expected) =>
        AgentSkillTasks.ToSkillName(value).Should().Be(expected);

    [Fact]
    public void Sync_CreatesTemplatesAndRefreshesOnlyTheCommandsBlock()
    {
        var root = TempRoot();
        try
        {
            var directory = Path.Combine(root, AgentSkillTasks.DefaultDirectory);
            AgentAction[] actions = [
                new("docs_build", "Builds the docs", "docs", "shop-docs") {
                    Parameters = [new AgentActionParameter("PORT", "int", "Port | number")],
                    Warnings = ["Slow"]
                },
                new("other", "Not required", "docs", null)
            ];
            var messages = new List<string>();

            AgentSkillTasks.Sync([directory], actions, messages.Add).Should().ContainSingle();

            var path = Path.Combine(directory, "shop-docs", "SKILL.md");
            var created = File.ReadAllText(path);
            created.Should().Contain("| `make docs_build` | Builds the docs **Warning:** Slow | `PORT` (int): Port \\| number |")
                .And.NotContain("make other");
            AgentSkillTasks.Validate([directory], actions).Should().ContainSingle().Which.Should().Contain("replace the TODO");

            File.WriteAllText(path, created.Replace("TODO: what these operations are for", "Docs tooling", StringComparison.Ordinal)
                .Replace("## Commands", "Custom notes.\n\n## Commands", StringComparison.Ordinal));
            AgentSkillTasks.Validate([directory], actions).Should().BeEmpty();

            AgentAction[] changed = [actions[0] with { Description = "Builds the site" }, actions[1]];
            AgentSkillTasks.Validate([directory], changed).Should().ContainSingle().Which.Should().Contain("out of date");
            AgentSkillTasks.Sync([directory], changed, messages.Add).Should().ContainSingle();
            var synced = File.ReadAllText(path);
            synced.Should().Contain("Builds the site").And.Contain("Custom notes.").And.Contain("description: \"Docs tooling");
            AgentSkillTasks.Validate([directory], changed).Should().BeEmpty();
            AgentSkillTasks.Sync([directory], changed).Should().BeEmpty();
            messages.Should().ContainMatch("Created*").And.ContainMatch("Updated*");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Validate_ReportsMissingAndInvalidSkillsAndIgnoresLinkedDirectories()
    {
        var root = TempRoot();
        try
        {
            void Write(string path, string content)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, path))!);
                File.WriteAllText(Path.Combine(root, path), content);
            }
            static string Skill(string name, string body = "") => $"---\nname: {name}\ndescription: 'It''s a skill'\n---\n{body}";
            AgentAction[] actions = [new("one", "One", "x", "shop-x"), new("two", "Two", "y", "shop-y"), new("three", "Three", "z", "shop-z")];

            Write("a/shop-x/SKILL.md", Skill("shop-x", "## When to use\n\n## Commands\n\n" + AgentSkillTasks.RenderCommands(actions[..1])));
            Write("a/shop-y/SKILL.md", Skill("shop-y", "## Commands\n"));
            Write("a/other/SKILL.md", Skill("wrong"));
            Write("a/Bad_Name/SKILL.md", Skill("Bad_Name"));
            Write("a/empty/SKILL.md", "no front matter");
            Write("b/shop-x/SKILL.md", Skill("shop-x"));
            if (!OperatingSystem.IsWindows())
                Directory.CreateSymbolicLink(Path.Combine(root, "b", "linked"), Path.Combine(root, "a", "shop-y"));

            var errors = AgentSkillTasks.Validate(
                [Path.Combine(root, "a"), Path.Combine(root, "b"), Path.Combine(root, "missing")], actions);

            errors.Should().HaveCount(7)
                .And.ContainMatch("Skill name 'shop-x' is used by*")
                .And.ContainMatch("*shop-y*missing the '## When to use' section")
                .And.ContainMatch("*shop-y*missing the commands block*")
                .And.ContainMatch("No skill 'shop-z' for make three*")
                .And.ContainMatch("*name 'wrong' must match its directory 'other'")
                .And.ContainMatch("*name 'Bad_Name' must be lowercase*")
                .And.ContainMatch("*empty*missing front matter*");
            AgentSkillTasks.ReadSkills([Path.Combine(root, "a")], []).Single(skill => skill.Name == "shop-x")
                .Description.Should().Be("It's a skill");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
