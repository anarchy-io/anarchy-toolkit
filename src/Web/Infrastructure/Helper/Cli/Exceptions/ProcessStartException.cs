using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.Exceptions;

/// <summary>
/// Thrown when an external process cannot be started.
/// </summary>
/// <param name="fileName">The executable that could not be started.</param>
public class ProcessStartException(string fileName)
    : ExternalDependencyException($"Could not start process '{fileName}'.")
{
    /// <summary>The executable that could not be started.</summary>
    public string FileName { get; } = fileName;
}
