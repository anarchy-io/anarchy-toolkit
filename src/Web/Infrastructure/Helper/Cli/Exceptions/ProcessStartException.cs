using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.Exceptions;

/// <summary>
/// Thrown when an external process cannot be started.
/// </summary>
public class ProcessStartException(string fileName)
    : ExternalDependencyException($"Could not start process '{fileName}'.")
{
    public string FileName { get; } = fileName;
}
