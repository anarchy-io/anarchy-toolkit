using Anarchy.Toolkit.Testing.MockServer.Client;

namespace Anarchy.Toolkit.Testing.MockServer.Fixture;

/// <summary>
/// An xUnit fixture giving tests a mock server: the one at <c>MOCKSERVER_URL</c> when set (e.g. started by the
/// Fallout <c>IMockServerBuild</c> targets), otherwise one spawned with <see cref="MockServerProcess"/>.
/// </summary>
/// <remarks>
/// Its state is reset when it starts. Derive from it to configure the spawned server through <see cref="Configure"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public sealed class OrdersMockServer : MockServerFixture
/// {
///     protected override void Configure(MockServerProcessOptions options) =&gt; options.ExpectationPaths.Add("mocks");
/// }
///
/// public class CheckoutTests(OrdersMockServer mock) : IClassFixture&lt;OrdersMockServer&gt;
/// {
///     [Fact]
///     public async Task Checkout_ReservesStock() =&gt; await mock.Client.AssertReceivedAsync("POST", "/reservations", times: 1);
/// }
/// </code>
/// </example>
public class MockServerFixture : IAsyncLifetime
{
    /// <summary>The environment variable naming an already running mock server.</summary>
    public const string UrlEnvironmentVariable = "MOCKSERVER_URL";

    private MockServerProcess? _process;

    /// <summary>A client for the server's control API.</summary>
    public MockServerClient Client { get; private set; } = null!;

    /// <summary>The server's base address.</summary>
    public Uri BaseAddress => Client.BaseAddress;

    /// <summary>Whether this fixture spawned the server (rather than connecting to <c>MOCKSERVER_URL</c>).</summary>
    public bool IsSpawned => _process is not null;

    /// <summary>Configures the spawned server; not used when connecting to <c>MOCKSERVER_URL</c>.</summary>
    /// <param name="options">The options.</param>
    protected virtual void Configure(MockServerProcessOptions options)
    {
    }

    /// <inheritdoc/>
    public virtual async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable(UrlEnvironmentVariable) is { Length: > 0 } url)
        {
            Client = new MockServerClient(new Uri(url));
            await Client.WaitUntilHealthyAsync(TimeSpan.FromSeconds(30));
        }
        else
        {
            var options = new MockServerProcessOptions();
            Configure(options);
            _process = await MockServerProcess.StartAsync(options);
            Client = _process.Client;
        }
        await Client.ResetAsync();
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_process is not null) await _process.DisposeAsync();
        else Client?.Dispose();
    }
}
