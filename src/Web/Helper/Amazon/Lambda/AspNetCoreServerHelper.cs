using Microsoft.AspNetCore.Http.Features;

namespace Anarchy.Toolkit.Web.Helper.Amazon.Lambda;

public static class AspNetCoreServerHelper
{
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
