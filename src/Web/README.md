# Anarchy.Toolkit.Web

Shared ASP.NET Core REST API pipeline and presentation helpers.

```bash
dotnet add package Anarchy.Toolkit.Web
```

- Build an API using `RestApiWebAppStartupBase` and the web entry-point bases for native hosting or AWS Lambda.
- Compose the supplied middleware, authentication, Swagger, JSON converters and validation attributes around your application's endpoints.
- A health controller exposes the shared health-report model. Application-specific scopes and endpoints remain in the consumer.

See the [endpoint guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/exposing-endpoints.md), [API sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Api) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
