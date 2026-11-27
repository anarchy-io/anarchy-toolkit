using Anarchy.Toolkit.Runtime.Web.MockServer.Entity;
using Anarchy.Toolkit.Runtime.Web.MockServer.Extensions.Microsoft.AspNetCore.Http;
using Anarchy.Toolkit.Runtime.Web.MockServer.Service;

namespace Anarchy.Toolkit.Runtime.Web.MockServer;

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
