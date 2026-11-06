# Anarchy.Toolkit.Web.GraphQL

GraphQL wiring for ASP.NET Core APIs built on the toolkit's web pipeline.

```bash
dotnet add package Anarchy.Toolkit.Web.GraphQL
```

- Define your schema using `SchemaBase` and register it with `AddToolkitGraphQl<TSchema>(graphTypesAssembly)`. Register root graph types separately.
- Add the endpoint and GraphiQL playground middleware with `UseToolkitGraphQl<TSchema>(authorizationPolicy)`.
- `GraphQlRoutes` provides route conventions; the GraphQL endpoint requires the supplied authorization policy.

See the [endpoint guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/exposing-endpoints.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
