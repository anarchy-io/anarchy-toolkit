using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Configuration.Extension.Microsoft.Hosting;

public class HostEnvironmentEnvExtensionTest
{
    [Theory]
    [InlineData(AppEnvironments.Development, true)]
    [InlineData(AppEnvironments.Testing, false)]
    [InlineData(AppEnvironments.Acceptance, false)]
    [InlineData(AppEnvironments.Staging, false)]
    [InlineData("sTaGiNg", false)]
    [InlineData(AppEnvironments.Production, false)]
    [InlineData("Custom", true)]
    public void IsLocalAndIsRemote_ClassifyEnvironment(string name, bool isLocal)
    {
        var environment = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == name);

        environment.IsLocal().Should().Be(isLocal);
        environment.IsRemote().Should().Be(!isLocal);
    }

    [Fact]
    public void AllAvailable_ContainsAllSupportedEnvironments()
    {
        AppEnvironments.AllAvailable.Should().BeEquivalentTo(
            AppEnvironments.Development,
            AppEnvironments.Testing,
            AppEnvironments.Acceptance,
            AppEnvironments.Staging,
            AppEnvironments.Production);
        AppEnvironments.Staging.Should().Be(Environments.Staging);
    }
}
