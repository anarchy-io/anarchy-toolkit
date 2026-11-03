using GraphQL.Types;

namespace Anarchy.Toolkit.Web.GraphQL.GraphQL;

/// <summary>
/// Base GraphQL schema for toolkit-based APIs. Derived schemas set <see cref="Schema.Query"/>
/// (and optionally mutations/subscriptions) resolving root graph types from DI.
/// </summary>
public abstract class SchemaBase(IServiceProvider serviceProvider) : Schema(serviceProvider);
