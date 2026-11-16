using Microsoft.AspNetCore.Http.Features;

namespace Anarchy.Toolkit.Web.Helper.Amazon.Lambda;

/// <summary>Helpers for hosting ASP.NET Core in AWS Lambda through <c>Amazon.Lambda.AspNetCoreServer</c>.</summary>
public static class AspNetCoreServerHelper
{
    /// <summary>
    /// Removes a leading path prefix (e.g. an API Gateway stage name) from the request path, case-insensitively.
    /// </summary>
    /// <remarks>
    /// The prefix is normalized to <c>/{prefix}</c>. If the resulting path is empty it becomes <c>/</c>. Nothing
    /// happens when <paramref name="prefix"/> is blank or the path does not start with it. Note that a plain
    /// <c>StartsWith</c> check is used, so <c>/dev</c> also matches <c>/devices</c>.
    /// </remarks>
    /// <param name="aspNetRequestFeature">The request feature whose <see cref="IHttpRequestFeature.Path"/> is modified.</param>
    /// <param name="prefix">The prefix to strip, with or without slashes.</param>
    public static void StripLeadingPathPrefix(
        IHttpRequestFeature aspNetRequestFeature,
        string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return;
        }

        prefix = "/" + prefix.Trim('/');

        if (!aspNetRequestFeature.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        aspNetRequestFeature.Path = aspNetRequestFeature.Path[prefix.Length..];

        if (string.IsNullOrWhiteSpace(aspNetRequestFeature.Path))
        {
            aspNetRequestFeature.Path = "/";
        }
    }
}
