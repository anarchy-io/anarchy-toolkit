using System.Diagnostics;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

public static class LocalProcessTasks
{
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

    public static string Require(string root, string executable, params string[] arguments)
    {
        var result = Run(root, executable, arguments);
        if (result.ExitCode != 0) throw new InvalidOperationException($"{executable} failed: {result.Error}");
        return result.Output;
    }
}
