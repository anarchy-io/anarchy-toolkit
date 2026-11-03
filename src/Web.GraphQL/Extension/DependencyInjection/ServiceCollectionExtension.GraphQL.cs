using GraphQL;
using GraphQL.Types;
using System.Reflection;

namespace Anarchy.Toolkit.Web.GraphQL.Extension.DependencyInjection;

public static partial class ServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the graphql-dotnet server for the given schema, using System.Text.Json and
        /// auto-discovering graph types from the supplied assembly.
        /// Root graph types (queries, object types) must still be registered by the caller.
        /// </summary>
        /// <typeparam name="TSchema">The schema exposed by the API.</typeparam>
        /// <param name="graphTypesAssembly">Assembly scanned for graph type registrations.</param>
        public IServiceCollection AddToolkitGraphQl<TSchema>(Assembly graphTypesAssembly)
            where TSchema : class, ISchema
        {
            return services.AddGraphQL(builder => builder
                .AddSchema<TSchema>()
                .AddSystemTextJson()
                .AddGraphTypes(graphTypesAssembly));
        }
    }
}
