using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Web.GraphQL.GraphQL;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Types;

namespace Anarchy.Toolkit.Web.GraphQL.Extension.Service.Builder;

public static partial class ApplicationBuilderExtension
{
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
        public IApplicationBuilder UseToolkitGraphQl<TSchema>(string authorizationPolicy,
                                                              string path = GraphQlRoutes.RouteBaseGraphQl,
                                                              string uiPath = GraphQlRoutes.RouteBaseGraphQlUi)
            where TSchema : ISchema
        {
            app.UseGraphQL<TSchema>(path.EnsureLeadingChar('/'), configureMiddleware: options => {
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
