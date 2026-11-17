# Anarchy.Toolkit.Testing.MockServer

Test helpers for [Anarchy.Toolkit.Web.MockServer](https://www.nuget.org/packages/Anarchy.Toolkit.Web.MockServer):
assert which requests the mock server received.

```bash
dotnet add package Anarchy.Toolkit.Testing.MockServer
```

- `MockServerClient` talks to the server's `/__mock` control API: received requests, runtime expectations, resets.
- `RecordedRequestAssertions` assert on what was received (`ShouldHaveReceived`, `ShouldNotHaveReceived`, `ShouldAllHaveBeenMatched`, `AssertReceivedAsync`), with failures listing the requests that were actually received.
- `RecordedRequestsFile.Read` reads the JSON-lines file written with `--record`, so you can assert after the server stopped.
- `MockServerFixture` connects to the server at `MOCKSERVER_URL` (set by the Fallout `IMockServerBuild` targets), or spawns one with `MockServerProcess`.

## When to use it

Use it in integration and end-to-end tests whose application calls HTTP services that the mock server stands in for.
Most of the time the mock server is started outside the tests (by Fallout or a Makefile), so tests only assert. Spawning
it from the tests is there for when they run on their own, e.g. from an IDE.

## How to use it

```csharp
public sealed class PaymentsMockServer : MockServerFixture
{
    // Only used when MOCKSERVER_URL is not set and the fixture spawns the server itself.
    protected override void Configure(MockServerProcessOptions options) => options.ExpectationPaths.Add("mocks");
}

public class CheckoutTests(PaymentsMockServer mock) : IClassFixture<PaymentsMockServer>
{
    [Fact]
    public async Task Checkout_CapturesThePayment()
    {
        await mock.Client.ResetAsync();
        await mock.Client.AddExpectationAsync("POST", "/payments/{id}/capture", 202);

        // … call the application, configured to reach the payments service at mock.BaseAddress …

        var captures = await mock.Client.AssertReceivedAsync("POST", "/payments/{id}/capture", times: 1);
        captures[0].BodyAs<CaptureRequest>()!.Amount.Should().Be(10m);
    }
}
```

Paths in assertions are exact, `{param}` templates or regular expressions, as in expectations. `MockServerProcess`
runs the `Anarchy.Toolkit.Web.MockServer` assembly copied next to the tests, or `dotnet run`s
`MockServerProcessOptions.ProjectPath` when you have a host of your own; it stops the server, and everything it started,
on dispose.

## Releases

### 1.0.0

- Initial release.
