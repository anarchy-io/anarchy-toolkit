using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Runtime.Web.Helpers.Cli.Exceptions;

/// <summary>
/// Thrown when an external process exits with a non-zero exit code.
/// </summary>
/// <param name="fileName">The executable that was run.</param>
/// <param name="exitCode">The non-zero exit code.</param>
/// <param name="error">The process standard error output, included in the message.</param>
public class ProcessExitCodeException(string fileName, int exitCode, string error)
    : ExternalDependencyException($"Process '{fileName}' failed with exit code {exitCode}: {error}")
{
    /// <summary>The executable that was run.</summary>
    public string FileName { get; } = fileName;
    /// <summary>The process exit code.</summary>
    public int ExitCode { get; } = exitCode;
}
