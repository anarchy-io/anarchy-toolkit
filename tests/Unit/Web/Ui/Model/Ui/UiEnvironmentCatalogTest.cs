using Anarchy.Toolkit.Web.Ui.Core.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Model.Ui;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Model.Ui;

public class UiEnvironmentCatalogTest
{
    private static UiEnvironmentCatalog CreateCatalog(string hostEnvironment)
    {
        var host = new Mock<IHostEnvironment>();
        host.SetupGet(x => x.EnvironmentName).Returns(hostEnvironment);

        return new UiEnvironmentCatalog(host.Object, new UiAuthenticationOptions {
            Environments = [
                new UiEnvironment("Development", new Uri("http://localhost:6001/")),
                new UiEnvironment("Testing", new Uri("https://auth.testing.example.com/"))
            ]
        });
    }

    [Fact]
    public void Names_ReturnsEnvironmentsInRegistrationOrder()
    {
        CreateCatalog("Development").Names.Should().Equal("Development", "Testing");
    }

    [Fact]
    public void GetByName_IgnoresCase()
    {
        CreateCatalog("Development").GetByName("testing").Name.Should().Be("Testing");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetByName_EmptyName_FallsBackToHostEnvironment(string? name)
    {
        CreateCatalog("Testing").GetByName(name).Name.Should().Be("Testing");
    }

    [Fact]
    public void GetByName_UnknownEnvironment_Throws()
    {
        var act = () => CreateCatalog("Development").GetByName("Production");

        act.Should().Throw<KeyNotFoundException>().WithMessage("*Production*");
    }
}
