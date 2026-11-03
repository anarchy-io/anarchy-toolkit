using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Entity.Api.Input;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Service.Host.Ui.Session;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Service.Host.Ui.Session;

public class UiSessionHandlerTest
{
    private readonly Mock<IUiEnvironmentCatalog> _catalog = new();

    private UiSessionHandler CreateHandler() => new(
        new UiAuthHandler(Mock.Of<IHttpClientFactory>(), InMemoryObserver.Create()),
        _catalog.Object,
        Mock.Of<IHostEnvironment>(x => x.EnvironmentName == "Development"),
        new UiAuthenticationOptions { LoginPath = "/app/login" });

    [Theory]
    [InlineData("", "secret")]
    [InlineData("client", " ")]
    public async Task HandleLoginAsync_MissingCredentials_RedirectsToLoginWithError(string clientId, string secret)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.PathBase = "/testing";

        // Act
        var result = await CreateHandler().HandleLoginAsync(context, new UiLoginRequest(clientId, secret, null));

        // Assert
        result.Should().BeOfType<RedirectHttpResult>()
            .Which.Url.Should().Be("/testing/app/login?error=" + UiLoginErrors.InvalidCredentials);
        _catalog.VerifyNoOtherCalls();
    }
}
