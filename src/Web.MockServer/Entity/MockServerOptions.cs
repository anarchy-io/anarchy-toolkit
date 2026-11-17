namespace Anarchy.Toolkit.Web.MockServer.Entity;

/// <summary>
/// The mock server's settings, read from the command line (see <see cref="Service.MockServerArguments"/>) and
/// <c>MOCKSERVER_*</c> environment variables.
/// </summary>
public sealed class MockServerOptions
{
    /// <summary>The port used when none is given; when it is taken, the next free one is used instead.</summary>
    public const int DefaultPort = 6001;

    /// <summary>The port to listen on; <see langword="null"/> means <see cref="DefaultPort"/> or the next free one.</summary>
    public int? Port { get; set; }

    /// <summary>The host name or address to listen on.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Expectation sources: JSON files, directories (every <c>*.json</c> below them) or glob patterns.</summary>
    public List<string> ExpectationPaths { get; } = [];

    /// <summary>Reloads expectations when a JSON file under <see cref="ExpectationPaths"/> changes.</summary>
    public bool WatchExpectations { get; set; }

    /// <summary>A file every received request is appended to, as one JSON object per line.</summary>
    public string? RecordFile { get; set; }

    /// <summary>How many requests are kept in memory for <c>/__mock/requests</c>; <c>0</c> keeps them all.</summary>
    public int RecordLimit { get; set; } = 1000;

    /// <summary>How many log entries are kept in memory for the UI and <c>/__mock/logs</c>; <c>0</c> keeps them all.</summary>
    public int LogBuffer { get; set; } = 1000;

    /// <summary>
    /// Serves the UI when the host plugs one in (e.g. <c>Anarchy.Toolkit.Web.MockServer.Ui</c>); <c>--no-ui</c> turns
    /// it off. Ignored by hosts without a UI.
    /// </summary>
    public bool Ui { get; set; } = true;

    /// <summary>Stops the server after this long, guarding against orphaned processes; <see langword="null"/> never stops.</summary>
    public TimeSpan? KillTimeout { get; set; }

    /// <summary>The status code answered when no expectation matches.</summary>
    public int UnmatchedStatusCode { get; set; } = 404;

    /// <summary>How much is written to the console.</summary>
    public MockServerVerbosity Verbosity { get; set; } = MockServerVerbosity.Normal;

    /// <summary>Disables colours in the console output (also disabled by the <c>NO_COLOR</c> environment variable).</summary>
    public bool NoColor { get; set; }
}
