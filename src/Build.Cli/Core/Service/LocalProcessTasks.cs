using System.Diagnostics;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>
/// Runs local processes such as <c>git</c>.
/// </summary>
public static class LocalProcessTasks
{
    /// <summary>
    /// Runs <paramref name="executable"/> in <paramref name="root"/> and waits for it to exit. With
    /// <paramref name="capture"/>, its output and error are returned; otherwise they go to the console and come back empty.
    /// </summary>
    /// <param name="root">The working directory.</param>
    /// <param name="executable">The executable to run, resolved through <c>PATH</c>.</param>
    /// <param name="arguments">The arguments, passed without shell interpretation.</param>
    /// <param name="environment">Extra environment variables, or <see langword="null"/>.</param>
    /// <param name="capture">Whether to capture the output and error rather than let them go to the console.</param>
    /// <returns>The exit code, and the captured output and error (empty without <paramref name="capture"/>).</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when the process could not be started.</exception>
    /// <exception cref="System.ComponentModel.Win32Exception">Thrown when <paramref name="executable"/> cannot be found.</exception>
    public static (int ExitCode, string Output, string Error) Run(string root, string executable, IEnumerable<string> arguments,
        IReadOnlyDictionary<string, string>? environment = null, bool capture = true)
    {
        var start = new ProcessStartInfo(executable) {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = capture,
            RedirectStandardError = capture
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        if (environment != null)
            foreach (var (key, value) in environment) start.Environment[key] = value;
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {executable}.");
        var output = capture ? process.StandardOutput.ReadToEndAsync() : Task.FromResult("");
        var error = capture ? process.StandardError.ReadToEndAsync() : Task.FromResult("");
        process.WaitForExit();
        return (process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    /// <summary>
    /// Runs <paramref name="executable"/> in <paramref name="root"/> and returns its output.
    /// </summary>
    /// <param name="root">The working directory.</param>
    /// <param name="executable">The executable to run.</param>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The captured standard output.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when the process exits with a non-zero code.</exception>
    public static string Require(string root, string executable, params string[] arguments)
    {
        var result = Run(root, executable, arguments);
        if (result.ExitCode != 0) throw new InvalidOperationException($"{executable} failed: {result.Error}");
        return result.Output;
    }
}
