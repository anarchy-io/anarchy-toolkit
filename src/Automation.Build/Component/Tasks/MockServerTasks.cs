using Fallout.Common.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Automation.Build.Component.Tasks;

/// <summary>
/// A mock server started in the background by <see cref="MockServerTasks.Start"/>, as saved to its state file.
/// </summary>
/// <param name="ProcessId">The process to stop.</param>
/// <param name="Port">The port it listens on.</param>
/// <param name="Url">Its base address.</param>
/// <param name="LogFile">The file its output goes to.</param>
public sealed record MockServerState(int ProcessId, int Port, string Url, string LogFile);

/// <summary>
/// Starts and stops an <c>Anarchy.Toolkit.Runtime.Web.MockServer</c> host (a project running <c>MockServerEntryPoint</c>) in
/// the background, so it outlives the build that started it until <see cref="Stop"/>.
/// </summary>
/// <remarks>Used by <see cref="IMockServerBuild"/>.</remarks>
public static partial class MockServerTasks
{
    /// <summary>The prefix of the line the mock server prints once it listens.</summary>
    public const string ReadyPrefix = "MOCKSERVER_READY";

    /// <summary>
    /// The mock server's command-line arguments.
    /// </summary>
    /// <param name="port">The port, or <see langword="null"/> for its default.</param>
    /// <param name="expectations">Expectation files, directories or globs.</param>
    /// <param name="recordFile">The record file, or <see langword="null"/>.</param>
    /// <param name="verbosity">The verbosity, or <see langword="null"/> for its default.</param>
    /// <param name="extra">Extra arguments, split on spaces.</param>
    /// <param name="ui">Whether hosts with a UI serve it; <see langword="false"/> passes <c>--no-ui</c>.</param>
    /// <returns>The arguments.</returns>
    public static IReadOnlyList<string> Arguments(
        int? port, IEnumerable<string> expectations, string? recordFile, string? verbosity, string? extra, bool ui = true)
    {
        var args = new List<string>();
        if (port is { } p) args.AddRange(["--port", p.ToString()]);
        foreach (var expectation in expectations) args.AddRange(["--expectations", expectation]);
        if (!string.IsNullOrWhiteSpace(recordFile)) args.AddRange(["--record", recordFile]);
        if (!string.IsNullOrWhiteSpace(verbosity)) args.AddRange(["--verbosity", verbosity]);
        if (!ui) args.Add("--no-ui");
        if (!string.IsNullOrWhiteSpace(extra)) args.AddRange(extra.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return args;
    }

    /// <summary>Reads the port and URL from a ready line.</summary>
    /// <param name="line">An output line.</param>
    /// <returns>The port and URL, or <see langword="null"/> when it is not the ready line.</returns>
    public static (int Port, string Url)? ParseReady(string line) =>
        ReadyLine().Match(line) is { Success: true } match ? (int.Parse(match.Groups["port"].Value), match.Groups["url"].Value) : null;

    /// <summary>
    /// Runs <paramref name="project"/> with <c>dotnet run --no-build</c> in the background, its output going to
    /// <paramref name="logFile"/>, waits for its ready line and saves its state to <paramref name="stateFile"/>.
    /// </summary>
    /// <param name="project">The mock server host project, already built in <paramref name="configuration"/>.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="arguments">The mock server's arguments (see <see cref="Arguments"/>).</param>
    /// <param name="workingDirectory">The directory relative paths in the arguments are resolved against.</param>
    /// <param name="stateFile">Where to save the state.</param>
    /// <param name="logFile">Where its output goes.</param>
    /// <param name="timeout">How long to wait for it to be ready.</param>
    /// <returns>Its state.</returns>
    /// <exception cref="InvalidOperationException">One is already running, or it exited before being ready.</exception>
    /// <exception cref="TimeoutException">It was not ready in time (it is stopped).</exception>
    public static MockServerState Start(
        AbsolutePath project, string configuration, IReadOnlyList<string> arguments, AbsolutePath workingDirectory,
        AbsolutePath stateFile, AbsolutePath logFile, TimeSpan timeout)
    {
        if (Read(stateFile) is { } running)
            throw new InvalidOperationException($"A mock server is already running at {running.Url} (process {running.ProcessId}); stop it first.");

        logFile.Parent.CreateDirectory();
        File.WriteAllText(logFile, "");

        var command = new[] { "dotnet", "run", "--project", project.ToString(), "--configuration", configuration, "--no-build", "--no-launch-profile", "--" }
            .Concat(arguments).ToList();
        var start = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe", $"/c \"{string.Join(' ', command.Select(QuoteWindows))} > {QuoteWindows(logFile)} 2>&1\"")
            : new ProcessStartInfo("/bin/sh", ["-c", $"exec {string.Join(' ', command.Select(QuoteUnix))} > {QuoteUnix(logFile)} 2>&1"]);
        start.UseShellExecute = false;
        start.WorkingDirectory = workingDirectory;
        start.Environment["NO_COLOR"] = "1";

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the mock server.");
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            var output = ReadShared(logFile);
            if (output.Split('\n').Select(line => ParseReady(line.Trim())).FirstOrDefault(r => r is not null) is { } ready)
            {
                var state = new MockServerState(process.Id, ready.Port, ready.Url, logFile);
                stateFile.Parent.CreateDirectory();
                File.WriteAllText(stateFile, JsonSerializer.Serialize(state));
                return state;
            }
            if (process.HasExited)
                throw new InvalidOperationException($"The mock server exited with code {process.ExitCode} before being ready:\n{output}");
            if (DateTime.UtcNow > deadline)
            {
                Kill(process.Id);
                throw new TimeoutException($"The mock server was not ready within {timeout.TotalSeconds:0}s:\n{output}");
            }
            Thread.Sleep(200);
        }
    }

    /// <summary>The state of the mock server started with <paramref name="stateFile"/>, when it is still running.</summary>
    /// <param name="stateFile">The state file.</param>
    /// <returns>The state, or <see langword="null"/> when none is running (a stale file is deleted).</returns>
    public static MockServerState? Read(AbsolutePath stateFile)
    {
        if (!File.Exists(stateFile)) return null;
        var state = JsonSerializer.Deserialize<MockServerState>(File.ReadAllText(stateFile));
        if (state is not null && IsRunning(state.ProcessId)) return state;
        File.Delete(stateFile);
        return null;
    }

    /// <summary>Stops the mock server started with <paramref name="stateFile"/>, and everything it started.</summary>
    /// <param name="stateFile">The state file.</param>
    /// <returns>The stopped server's state, or <see langword="null"/> when none was running.</returns>
    public static MockServerState? Stop(AbsolutePath stateFile)
    {
        var state = Read(stateFile);
        if (state is null) return null;
        Kill(state.ProcessId);
        File.Delete(stateFile);
        return state;
    }

    private static bool IsRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void Kill(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.Kill(entireProcessTree: true);
            process.WaitForExit(10_000);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            // Already exited.
        }
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string QuoteUnix(string value) => "'" + value.Replace("'", "'\\''") + "'";

    private static string QuoteWindows(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    [GeneratedRegex($@"^{ReadyPrefix} port=(?<port>\d+) url=(?<url>\S+)")]
    private static partial Regex ReadyLine();
}
