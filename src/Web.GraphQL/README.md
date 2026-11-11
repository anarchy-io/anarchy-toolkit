# Anarchy.Toolkit.Web.GraphQL

GraphQL wiring for ASP.NET Core APIs built on the toolkit's web pipeline.

```bash
dotnet add package Anarchy.Toolkit.Web.GraphQL
```

- Define your schema using `SchemaBase` and register it with `AddToolkitGraphQl<TSchema>(graphTypesAssembly)`. Register root graph types separately.
- Add the endpoint and GraphiQL playground middleware with `UseToolkitGraphQl<TSchema>(authorizationPolicy)`.
- `GraphQlRoutes` provides route conventions; the GraphQL endpoint requires the supplied authorization policy.

## When to use it

Add this package when an ASP.NET Core application needs a GraphQL endpoint alongside, or instead of, REST routes. It wires graphql-dotnet into the host; your application supplies its schema, query and mutation types, resolvers and authorization policy.

## How to use it

1. Define a schema (optionally deriving from `SchemaBase`) and set its query and any mutation or subscription roots. Register root graph types and their application services in DI.
2. Register the schema and discover graph types from the appropriate assembly. Add the middleware with the name of an authorization policy already configured by the host:

```csharp
using Anarchy.Toolkit.Web.GraphQL.Extension.DependencyInjection;
using Anarchy.Toolkit.Web.GraphQL.Extension.Service.Builder;

services.AddToolkitGraphQl<MySchema>(typeof(MySchema).Assembly);
// In the configured ASP.NET Core pipeline:
app.UseToolkitGraphQl<MySchema>("MyGraphQlPolicy");
```

Replace `MySchema` and the policy name with your application types. Optional path arguments override the endpoint and GraphiQL routes. The GraphQL endpoint requires the supplied policy; evaluate whether the separately served GraphiQL UI should be exposed in each environment.

See the [endpoint guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/exposing-endpoints.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced a GraphQL schema base, route definitions and extensions for exposing GraphQL endpoints.
