using Anarchy.Toolkit.Core.Observability.Service.Observability;
using CbTagBuilder = Anarchy.Toolkit.Core.Observability.Service.Observability.TagBuilderBase;

namespace Anarchy.Toolkit.Web.Extension.Service.Observability.TagBuilderBase;

/// <summary>
/// HTTP-specific extensions for the observability tag builder
/// (<see cref="Anarchy.Toolkit.Core.Observability.Service.Observability.TagBuilderBase"/>), used to enrich metrics
/// recorded by web middlewares with status code, controller, action and request information.
/// </summary>
/// <remarks>
/// Used by <see cref="Anarchy.Toolkit.Web.Service.Host.Middleware.LoggingMiddleware"/> to tag the request duration metric.
/// Controller and action names are read from the route pattern defaults of the matched <see cref="RouteEndpoint"/>;
/// when no route endpoint is matched the builder is returned unchanged.
/// </remarks>
public static class MiddlewareTagsExtensions
{
    extension(CbTagBuilder tags)
    {
        /// <summary>
        /// Creates a tag builder starting from <c>Tags.Success</c> for 2xx status codes or <c>Tags.Failure</c>
        /// otherwise, and adds the <c>status_code</c> tag.
        /// </summary>
        /// <param name="httpStatusCode">The HTTP response status code.</param>
        /// <returns>A new tag builder describing the outcome.</returns>
        public static CbTagBuilder FromStatusCode(int httpStatusCode)
        {
            return (httpStatusCode is >= 200 and < 300 ? Tags.Success : Tags.Failure)
                .WithStatusCode(httpStatusCode);
        }

        /// <summary>Adds the <c>status_code</c> tag.</summary>
        /// <param name="httpStatusCode">The HTTP response status code.</param>
        /// <returns>The tag builder, for chaining.</returns>
        public CbTagBuilder WithStatusCode(int httpStatusCode)
        {
            return tags.WithTag("status_code", httpStatusCode.ToString());
        }

        /// <summary>Adds the <c>controller</c> tag from the matched endpoint's route defaults, when available.</summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The tag builder, for chaining.</returns>
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

        /// <summary>Adds the <c>controller_action</c> tag from the matched endpoint's route defaults, when available.</summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The tag builder, for chaining.</returns>
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

        /// <summary>Adds the <c>request_method</c> tag (e.g. <c>GET</c>).</summary>
        /// <param name="httpRequest">The current HTTP request.</param>
        /// <returns>The tag builder, for chaining.</returns>
        public CbTagBuilder WithRequestMethod(HttpRequest httpRequest)
        {
            return tags.WithTag("request_method", httpRequest.Method);
        }

        /// <summary>Adds the <c>request_path</c> tag using the request's <see cref="HttpRequest.PathBase"/>.</summary>
        /// <remarks>Beware of metric cardinality when tagging paths.</remarks>
        /// <param name="httpRequest">The current HTTP request.</param>
        /// <returns>The tag builder, for chaining.</returns>
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
