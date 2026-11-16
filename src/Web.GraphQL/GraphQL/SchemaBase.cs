using GraphQL.Types;

namespace Anarchy.Toolkit.Web.GraphQL.GraphQL;

/// <summary>
/// Base GraphQL schema for toolkit-based APIs. Derived schemas set <see cref="Schema.Query"/>
/// (and optionally mutations/subscriptions) resolving root graph types from DI.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve graph types.</param>
/// <example>
/// <code language="csharp">
/// public sealed class ApiSchema : SchemaBase
/// {
///     public ApiSchema(IServiceProvider serviceProvider) : base(serviceProvider)
///     {
///         Query = serviceProvider.GetRequiredService&lt;OrderQuery&gt;();
///     }
/// }
/// </code>
/// </example>
public abstract class SchemaBase(IServiceProvider serviceProvider) : Schema(serviceProvider);
