using Anarchy.Toolkit.Core.Observability.Service.Observability;
using CbTagBuilder = Anarchy.Toolkit.Core.Observability.Service.Observability.TagBuilderBase;

namespace Anarchy.Toolkit.Web.Extension.Service.Observability.TagBuilderBase;

public static class MiddlewareTagsExtensions
{
    extension(CbTagBuilder tags)
    {
        public static CbTagBuilder FromStatusCode(int httpStatusCode)
        {
            return (httpStatusCode is >= 200 and < 300 ? Tags.Success : Tags.Failure)
                .WithStatusCode(httpStatusCode);
        }

        public CbTagBuilder WithStatusCode(int httpStatusCode)
        {
            return tags.WithTag("status_code", httpStatusCode.ToString());
        }

        public CbTagBuilder WithController(HttpContext context)
        {
            var routeValues = CbTagBuilder.ExtractRouteValues(context);
            if (routeValues == null)
            {
                return tags;
            }

            return routeValues.TryGetValue("controller", out var controllerName)
                ? tags.WithTag("controller", (string)controllerName!)
                : tags;
        }

        public CbTagBuilder WithAction(HttpContext context)
        {
            var routeValues = CbTagBuilder.ExtractRouteValues(context);
            if (routeValues == null)
            {
                return tags;
            }

            return routeValues.TryGetValue("action", out var actionName)
                ? tags.WithTag("controller_action", (string)actionName!)
                : tags;
        }

        public CbTagBuilder WithRequestMethod(HttpRequest httpRequest)
        {
            return tags.WithTag("request_method", httpRequest.Method);
        }

        public CbTagBuilder WithRequestPath(HttpRequest httpRequest)
        {
            return tags.WithTag("request_path", httpRequest.PathBase);
        }

        private static IReadOnlyDictionary<string, object?>? ExtractRouteValues(HttpContext context)
        {
            return (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.Defaults;
        }
    }
}
