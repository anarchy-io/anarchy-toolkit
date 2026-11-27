using GraphQL;
using GraphQL.Types;
using System.Reflection;

namespace Anarchy.Toolkit.Runtime.Web.GraphQL.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions that register the toolkit GraphQL server.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <param name="services">The service collection to register into.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the graphql-dotnet server for the given schema, using System.Text.Json and
        /// auto-discovering graph types from the supplied assembly.
        /// Root graph types (queries, object types) must still be registered by the caller.
        /// </summary>
        /// <typeparam name="TSchema">The schema exposed by the API.</typeparam>
        /// <param name="graphTypesAssembly">Assembly scanned for graph type registrations.</param>
        /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
        /// <example>
        /// <code language="csharp">
        /// builder.Services
        ///     .AddSingleton&lt;OrderQuery&gt;()
        ///     .AddToolkitGraphQl&lt;ApiSchema&gt;(typeof(ApiSchema).Assembly);
        /// </code>
        /// </example>
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
