using Anarchy.Toolkit.Runtime.Web.Helpers.Cli.ProcessRunners;
using AwesomeAssertions;
using System.Runtime.InteropServices;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Helpers.Cli.ProcessRunners;

public class PlatformInfoRunnerTest
{
    [Fact]
    public async Task GetHostCpuArchitectureAsync_OnWindows_UsesOperatingSystemArchitecture()
    {
        if (!OperatingSystem.IsWindows()) return;

        var expected = RuntimeInformation.OSArchitecture switch {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "arm64",
            var other => other.ToString().ToLowerInvariant(),
        };

        var architecture = await PlatformInfoRunner.GetHostCpuArchitectureAsync(TestContext.Current.CancellationToken);

        architecture.Should().Be(expected);
        (await PlatformInfoRunner.IsArmHostAsync(TestContext.Current.CancellationToken)).Should().Be(expected == "arm64");
    }
}
