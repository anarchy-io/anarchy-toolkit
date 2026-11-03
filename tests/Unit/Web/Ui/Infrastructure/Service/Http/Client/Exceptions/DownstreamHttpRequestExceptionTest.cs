using Anarchy.Toolkit.Web.Ui.Infrastructure.Service.Http.Client.Exceptions;
using AwesomeAssertions;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Infrastructure.Service.Http.Client.Exceptions;

public class DownstreamHttpRequestExceptionTest
{
    private static HttpResponseMessage Response(HttpStatusCode statusCode, string body) => new(statusCode) {
        Content = new StringContent(body),
        RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/orders/1")
    };

    [Fact]
    public async Task CreateAsync_JsonBodyWithDetail_AppendsSummaryAndIndentsContent()
    {
        // Act
        var exception = await DownstreamHttpRequestException.CreateAsync(Response(HttpStatusCode.BadRequest, """{"title":"Bad","detail":"Order is locked"}"""), TestContext.Current.CancellationToken);

        // Assert
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Message.Should().EndWith(": Order is locked");
        exception.Message.Should().Contain("https://api.example.com/orders/1");
        exception.ResponseJson.Should().NotBeNull();
        exception.ResponseContent.Should().Contain(System.Environment.NewLine);
    }

    [Fact]
    public async Task CreateAsync_PlainTextBody_KeepsRawBody()
    {
        var exception = await DownstreamHttpRequestException.CreateAsync(Response(HttpStatusCode.InternalServerError, "boom"), TestContext.Current.CancellationToken);

        exception.ResponseContent.Should().Be("boom");
        exception.ResponseJson.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_EmptyBody_UsesStatusLine()
    {
        var exception = await DownstreamHttpRequestException.CreateAsync(Response(HttpStatusCode.NotFound, ""), TestContext.Current.CancellationToken);

        exception.ResponseContent.Should().Be("404 Not Found");
    }
}
