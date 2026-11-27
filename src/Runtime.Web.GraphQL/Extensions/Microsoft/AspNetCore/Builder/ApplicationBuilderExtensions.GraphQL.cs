using Anarchy.Toolkit.Core.Extensions.System;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Types;

namespace Anarchy.Toolkit.Runtime.Web.GraphQL.Extensions.Microsoft.AspNetCore.Builder;

/// <summary>
/// <see cref="IApplicationBuilder"/> extensions that expose a toolkit GraphQL API.
/// </summary>
public static partial class ApplicationBuilderExtensions
{
    /// <param name="app">The application builder.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Maps the GraphQL HTTP endpoint for the given schema and serves the GraphiQL playground UI.
        /// The GraphQL endpoint is gated by the supplied authorization policy so it matches the
        /// protection applied to the API's REST controllers.
        /// </summary>
        /// <typeparam name="TSchema">The schema exposed by the API.</typeparam>
        /// <param name="authorizationPolicy">Authorization policy required to call the GraphQL endpoint.</param>
        /// <param name="path">The GraphQL endpoint path.</param>
        /// <param name="uiPath">The GraphiQL UI path.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/>, for chaining.</returns>
        /// <remarks>
        /// <para>
        /// A leading <c>/</c> is added to <paramref name="path"/> and <paramref name="uiPath"/> when missing. Defaults
        /// are <see cref="GraphQlRoutes.RouteBaseGraphQl"/> (<c>/graphql</c>) and
        /// <see cref="GraphQlRoutes.RouteBaseGraphQlUi"/> (<c>/ui/graphql</c>).
        /// </para>
        /// <para>
        /// Only the GraphQL endpoint requires authorization; the GraphiQL UI is served without it. Call after
        /// authentication/authorization middleware and register the schema with <c>AddToolkitGraphQl</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddToolkitGraphQl&lt;ApiSchema&gt;(typeof(ApiSchema).Assembly);
        /// var app = builder.Build();
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// app.UseToolkitGraphQl&lt;ApiSchema&gt;("ApiPolicy");
        /// </code>
        /// </example>
        public IApplicationBuilder UseToolkitGraphQl<TSchema>(string authorizationPolicy,
                                                              string path = GraphQlRoutes.RouteBaseGraphQl,
                                                              string uiPath = GraphQlRoutes.RouteBaseGraphQlUi)
            where TSchema : ISchema
        {
            app.UseGraphQL<TSchema>(path.EnsureLeadingChar('/'),
                configureMiddleware: options => {
                    options.AuthorizationRequired = true;
                    options.AuthorizedPolicy = authorizationPolicy;
                });

            app.UseGraphQLGraphiQL(
                uiPath.EnsureLeadingChar('/'),
                new GraphiQLOptions {
                    GraphQLEndPoint = path.EnsureLeadingChar('/'),
                    SubscriptionsEndPoint = path.EnsureLeadingChar('/')
                });

            return app;
        }
    }
}
