# Anarchy.Toolkit.Infrastructure.Http

Reusable HTTP client and repository plumbing for service integrations.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Http
```

- Derive client repositories from `HttpRepositoryBase` or `ScopedHttpRepositoryBase`.
- Configure clients and compose the supplied delegating handlers, including timeout and MSAL certificate authentication handlers, as needed.
- Keep application-specific endpoints and client behavior in the consuming service.

See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme) and [architecture guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md).
