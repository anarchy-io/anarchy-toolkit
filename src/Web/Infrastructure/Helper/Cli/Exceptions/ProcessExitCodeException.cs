using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.Exceptions;

/// <summary>
/// Thrown when an external process exits with a non-zero exit code.
/// </summary>
public class ProcessExitCodeException(string fileName, int exitCode, string error)
    : ExternalDependencyException($"Process '{fileName}' failed with exit code {exitCode}: {error}")
{
    public string FileName { get; } = fileName;
    public int ExitCode { get; } = exitCode;
}
