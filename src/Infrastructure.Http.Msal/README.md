# Anarchy.Toolkit.Infrastructure.Http.Msal

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Opt-in Microsoft Entra ID (MSAL) authentication for HTTP clients built on `Anarchy.Toolkit.Infrastructure.Http`.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Http.Msal
```

- `MsalCertificateAuthenticationHandler` acquires an OAuth 2.0 bearer token with certificate-based client credentials and adds it to every request.
- `MsalCertificateHttpClientConfiguration<TClient>` adds the handler on top of the common client configuration, driven by `IMsalCertificateHttpAdapterSettings<TClient>`.

## When to use it

Use this package only when an adapter calls an API protected by Microsoft Entra ID with certificate credentials. It keeps
`Microsoft.Identity.Client` out of `Infrastructure.Http` for everyone else.

## How to use it

1. Implement `IMsalCertificateHttpAdapterSettings<TClient>` with the client id, authority, scope and PEM certificate and private key.
2. Register `MsalCertificateHttpClientConfiguration<TClient>` as you would any `HttpClientConfigurationBase<TClient>`.
3. Handle `AccessTokenAcquisitionException` and `CertificatePrivateKeyMissingException` at the adapter boundary as needed.

See the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/) and [architecture guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
