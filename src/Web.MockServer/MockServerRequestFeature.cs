using Anarchy.Toolkit.Web.MockServer.Entity;
using Anarchy.Toolkit.Web.MockServer.Service;

namespace Anarchy.Toolkit.Web.MockServer;

/// <summary>
/// The mock server's view of the current request, available to consumer middlewares through
/// <see cref="MockServerHttpContextExtensions.GetMockServer"/>.
/// </summary>
/// <remarks>
/// <see cref="Expectation"/> is the match found before the consumer middlewares run. A middleware may replace it (to
/// answer with another expectation), clear it, or answer the request itself and set <see cref="AnsweredBy"/> so the
/// recording and console output say who answered.
/// </remarks>
public sealed class MockServerRequestFeature
{
    internal MockServerRequestFeature(MockRequest request) => Request = request;

    /// <summary>The request, with its body already read.</summary>
    public MockRequest Request { get; internal set; }

    /// <summary>The expectation that will answer, or <see langword="null"/> when none matched.</summary>
    public MockExpectation? Expectation { get; set; }

    /// <summary>Who answered: an expectation's display name, or any label a middleware sets.</summary>
    public string? AnsweredBy { get; set; }
}

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
