using Anarchy.Toolkit.Runtime.Web.Aws.Extensions.Microsoft.AspNetCore.Builder;

namespace Anarchy.Toolkit.Runtime.Web.Aws.Composition;

/// <summary>
/// Puts the AWS X-Ray trace id middleware (<c>UseAmazonTraceId</c>) in front of the application's pipeline.
/// </summary>
internal sealed class AmazonTraceIdStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app => {
            app.UseAmazonTraceId();
            next(app);
        };
    }
}
