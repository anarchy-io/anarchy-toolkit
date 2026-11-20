# Anarchy.Toolkit.Testing.MockServer

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Test helpers for [Anarchy.Toolkit.Web.MockServer](https://www.nuget.org/packages/Anarchy.Toolkit.Web.MockServer):
assert which requests the mock server received.

```bash
dotnet add package Anarchy.Toolkit.Testing.MockServer
```

- `MockServerClient` talks to the server's `/__mock` control API: received requests, runtime expectations, resets.
- `RecordedRequestAssertions` assert on what was received (`ShouldHaveReceived`, `ShouldNotHaveReceived`, `ShouldAllHaveBeenMatched`, `AssertReceivedAsync`), with failures listing the requests that were actually received.
- `RecordedRequestsFile.Read` reads the JSON-lines file written with `--record`, so you can assert after the server stopped.
- `MockServerFixture` connects to the server at `MOCKSERVER_URL` (set by the Anarchy `IMockServerBuild` targets), or spawns one with `MockServerProcess`.

## When to use it

Use it in integration and end-to-end tests whose application calls HTTP services that the mock server stands in for.
Most of the time the mock server is started outside the tests (by Anarchy automation or a Makefile), so tests only assert. Spawning
it from the tests is there for when they run on their own, e.g. from an IDE.

## How to use it

```csharp
public sealed class IdentityMockServer : MockServerFixture
{
    // Only used when MOCKSERVER_URL is not set and the fixture spawns the server itself.
    protected override void Configure(MockServerProcessOptions options) => options.ExpectationPaths.Add("mocks");
}

public class UserRegistrationTests(IdentityMockServer mock) : IClassFixture<IdentityMockServer>
{
    [Fact]
    public async Task Register_CreatesTheUserInTheIdentityService()
    {
        await mock.Client.ResetAsync();
        await mock.Client.AddExpectationAsync("POST", "/clients/{id}/users", 201);

        // … call the application, configured to reach the identity service at mock.BaseAddress …

        var requests = await mock.Client.AssertReceivedAsync("POST", "/clients/{id}/users", times: 1);
        requests[0].BodyAs<CreateUserRequest>()!.Email.Should().Be("jane.doe@example.com");
    }
}
```

Paths in assertions are exact, `{param}` templates or regular expressions, as in expectations. `MockServerProcess`
runs the `Anarchy.Toolkit.Web.MockServer` assembly copied next to the tests, or `dotnet run`s
`MockServerProcessOptions.ProjectPath` when you have a host of your own; it stops the server, and everything it started,
on dispose.

## Releases

### 1.2.0

- feat(Core.Telemetry): add provider-neutral telemetry facade
- feat(Core.Telemetry): route optional metrics through application logging
- feat(Configuration): register provider-neutral application telemetry

### 1.1.0

- feat(Web.MockServer): keep an in-memory log and describe plugged-in middlewares

### 1.0.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata
- fix(mock-server): default to port 6001, which browsers allow

### 1.0.0

- Initial release.
