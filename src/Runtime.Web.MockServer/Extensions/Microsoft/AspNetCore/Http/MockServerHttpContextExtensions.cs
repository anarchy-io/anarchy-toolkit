namespace Anarchy.Toolkit.Runtime.Web.MockServer.Extensions.Microsoft.AspNetCore.Http;

/// <summary>
/// Mock server helpers on <see cref="HttpContext"/>.
/// </summary>
public static class MockServerHttpContextExtensions
{
    /// <summary>The mock server's view of the request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The feature.</returns>
    /// <exception cref="InvalidOperationException">The request is not handled by the mock server.</exception>
    public static MockServerRequestFeature GetMockServer(this HttpContext context) =>
        context.Features.Get<MockServerRequestFeature>()
        ?? throw new InvalidOperationException("This request is not handled by the mock server.");
}
