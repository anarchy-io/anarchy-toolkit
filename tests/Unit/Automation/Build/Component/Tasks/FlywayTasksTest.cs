using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Core.Service.Container;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Component.Tasks;

public class FlywayTasksTest : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("flyway-test-").FullName;
    private readonly FakeCli _cli = new();

    [Theory]
    [InlineData("migrate")]
    [InlineData("info")]
    public void Run_MountsConfigFilesAndMigrationLocationsInOrder(string command)
    {
        var baseConfig = ConfigFile("base.toml");
        var localConfig = ConfigFile("local config,dev.toml");
        var sharedMigrations = Directory.CreateDirectory(Path.Combine(_root, "shared migrations")).FullName;
        var localMigrations = Directory.CreateDirectory(Path.Combine(_root, "local migrations")).FullName;

        FlywayTasks.Run(_cli, _root, "custom/flyway:11", "example_default",
            ["base.toml", localConfig], ["shared migrations", localMigrations], command);

        var call = _cli.Calls.Should().ContainSingle().Subject;
        call.Executable.Should().Be("docker");
        call.Arguments.Should().Equal(
            "run", "--rm", "--network", "example_default",
            "--mount", $"type=bind,source={baseConfig},target=/flyway/anarchy/config/0.toml,readonly",
            "--mount", $"type=bind,\"source={localConfig}\",target=/flyway/anarchy/config/1.toml,readonly",
            "--mount", $"type=bind,source={sharedMigrations},target=/flyway/anarchy/sql/0,readonly",
            "--mount", $"type=bind,source={localMigrations},target=/flyway/anarchy/sql/1,readonly",
            "custom/flyway:11", "-configFiles=/flyway/anarchy/config/0.toml,/flyway/anarchy/config/1.toml",
            "-locations=filesystem:/flyway/anarchy/sql/0,filesystem:/flyway/anarchy/sql/1", command);
    }

    [Fact]
    public void Run_LeavesNetworkAndLocationsToDockerAndFlywayWhenOmitted()
    {
        var config = ConfigFile("database.conf");

        FlywayTasks.Run(_cli, _root, "custom/flyway:11", null, [config], [], "info");

        _cli.Calls.Should().ContainSingle().Subject.Arguments.Should().Equal("run", "--rm",
            "--mount", $"type=bind,source={config},target=/flyway/anarchy/config/0.conf,readonly",
            "custom/flyway:11", "-configFiles=/flyway/anarchy/config/0.conf", "info");
    }

    [Theory]
    [InlineData("migrate")]
    [InlineData("info")]
    public void Run_FailsOnNonzeroExit(string command)
    {
        var config = ConfigFile("database.conf");
        _cli.ExitCode = 7;

        Action run = () => FlywayTasks.Run(_cli, _root, "custom/flyway:11", null, [config], [], command);

        run.Should().Throw<InvalidOperationException>().WithMessage($"Flyway {command} failed with exit code 7.");
    }

    [Fact]
    public void Run_DoesNotStartDockerWithMissingFilesOrDirectories()
    {
        Action noConfig = () => FlywayTasks.Run(_cli, _root, "custom/flyway:11", null, [], [], "migrate");
        Action missingConfig = () => FlywayTasks.Run(_cli, _root, "custom/flyway:11", null, ["missing.conf"], [], "migrate");
        var config = ConfigFile("database.conf");
        Action missingLocation = () => FlywayTasks.Run(_cli, _root, "custom/flyway:11", null, [config], ["missing"], "info");

        noConfig.Should().Throw<InvalidOperationException>().WithMessage("*FlywayConfigFiles*");
        missingConfig.Should().Throw<InvalidOperationException>().WithMessage("*configuration file not found*missing.conf");
        missingLocation.Should().Throw<InvalidOperationException>().WithMessage("*migration directory not found*missing");
        _cli.Calls.Should().BeEmpty();
    }

    public void Dispose() => TestDirectory.Delete(_root);

    private string ConfigFile(string name)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private sealed class FakeCli : IContainerCli
    {
        public int ExitCode { get; set; }
        public List<(string Executable, IReadOnlyList<string> Arguments)> Calls { get; } = [];

        public (int ExitCode, string Output, string Error) Capture(string executable, IReadOnlyList<string> arguments,
            IReadOnlyDictionary<string, string>? environment = null) => throw new NotSupportedException();

        public int Stream(string executable, IReadOnlyList<string> arguments,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            Calls.Add((executable, arguments));
            return ExitCode;
        }
    }
}
