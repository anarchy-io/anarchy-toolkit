using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Web.Fixture;
using Anarchy.Toolkit.Web.Service.Host.Middleware;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.Unit.Web.Service.Host.Middleware;

public class ExceptionMiddlewareTest : TestCaseBase, IDisposable
{
    private readonly ExceptionMiddleware _sut;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly DefaultHttpContext _httpContextWithIgnoreAttribute;
    private readonly DefaultHttpContext _httpContextWithoutIgnoreAttribute;

    public ExceptionMiddlewareTest()
    {
        _mockNext = new Mock<RequestDelegate>();

        _sut = new ExceptionMiddleware(
            _mockNext.Object,
            InMemoryObserver.Create()
        );

        _httpContextWithIgnoreAttribute = HttpContextFixture.RetrieveIgnoredContext();
        _httpContextWithoutIgnoreAttribute = HttpContextFixture.RetrieveContext();
    }

    public void Dispose()
    {
        _httpContextWithIgnoreAttribute.Response.Body.Dispose();
        _httpContextWithoutIgnoreAttribute.Response.Body.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task InvokeAsync_WhenMiddleware_NotIgnored()
    {
        // Act
        await _sut.InvokeAsync(_httpContextWithoutIgnoreAttribute);

        // Assert
        _mockNext.Verify(next => next(_httpContextWithoutIgnoreAttribute), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenMiddleware_Ignored()
    {
        // Act
        await _sut.InvokeAsync(_httpContextWithIgnoreAttribute);

        // Assert
        _mockNext.Verify(next => next(_httpContextWithIgnoreAttribute), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNonIgnoredEndpoint_WillHaveCorrectValues()
    {
        // Arrange
        var customException = new TestCustomException("message", new Exception("foo"));

        var expectedBody = JsonSerializer.Serialize(new {
            finalException = "message",
            originalException = "foo",
            stack = new[] {
                $"[{typeof(TestCustomException).FullName}] message",
                "[System.Exception] foo"
            }
        });

        _mockNext.Setup(next => next(_httpContextWithoutIgnoreAttribute)).Throws(customException);

        // Act
        await _sut.InvokeAsync(_httpContextWithoutIgnoreAttribute);

        // Assert
        _httpContextWithoutIgnoreAttribute.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContextWithoutIgnoreAttribute.Response.Body);
        var responseBody = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        _httpContextWithoutIgnoreAttribute.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        responseBody.Should().Be(expectedBody);
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationExceptionThrows_WillReturnBadRequest()
    {
        // Arrange
        _mockNext.Setup(next => next(_httpContextWithoutIgnoreAttribute))
            .Throws(new InvalidOperationException("Invalid input"));

        // Act
        await _sut.InvokeAsync(_httpContextWithoutIgnoreAttribute);

        // Assert
        _httpContextWithoutIgnoreAttribute.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WithIgnoreAttribute_ThrowsProvidedException()
    {
        // Arrange
        _mockNext.Setup(next => next(_httpContextWithIgnoreAttribute)).Throws(new ArgumentOutOfRangeException());

        // Act
        var act = async () => await _sut.InvokeAsync(_httpContextWithIgnoreAttribute);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task InvokeAsync_WhenNonCustomExceptionThrows_WillReturnInternalServerError()
    {
        _mockNext.Setup(next => next(_httpContextWithoutIgnoreAttribute))
            .Throws(new ApplicationException("Something broke"));

        var expectedBody = new {
            finalException = "Something broke",
            originalException = "Something broke",
            stack = new[] {
                "[System.ApplicationException] Something broke"
            }
        };

        // Act
        await _sut.InvokeAsync(_httpContextWithoutIgnoreAttribute);

        // Assert
        _httpContextWithoutIgnoreAttribute.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContextWithoutIgnoreAttribute.Response.Body);
        var responseBody = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        _httpContextWithoutIgnoreAttribute.Response.StatusCode.Should().Be(500);
        responseBody.Should().Be(JsonSerializer.Serialize(expectedBody));
    }
}
