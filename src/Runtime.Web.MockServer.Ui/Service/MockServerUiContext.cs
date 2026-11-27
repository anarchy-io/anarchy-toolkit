using Anarchy.Toolkit.Runtime.Web.MockServer.Entity;
using Anarchy.Toolkit.Runtime.Web.MockServer.Service;

namespace Anarchy.Toolkit.Runtime.Web.MockServer.Ui.Service;

/// <summary>
/// What the UI pages show: the running <see cref="MockServerHost"/>, its expectations, requests, middlewares and log.
/// </summary>
public sealed class MockServerUiContext
{
    private MockServerHost? _host;

    /// <summary>The running server.</summary>
    /// <exception cref="InvalidOperationException">The server is not built yet.</exception>
    public MockServerHost Host {
        get => _host ?? throw new InvalidOperationException("The mock server is not running yet.");
        internal set => _host = value;
    }

    /// <summary>The expectations answered with.</summary>
    public ExpectationStore Expectations => Host.Expectations;

    /// <summary>The received requests.</summary>
    public RequestRecorder Recorder => Host.Recorder;

    /// <summary>The consumer middlewares, in pipeline order.</summary>
    public IReadOnlyList<MockServerMiddleware> Middlewares => Host.Middlewares;

    /// <summary>The in-memory log.</summary>
    public MockServerLog Log => Host.Log;

    /// <summary>The options in use.</summary>
    public MockServerOptions Options => Host.Options;
}
