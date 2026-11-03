namespace Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting.Exceptions;

/// <summary>
/// Thrown when a startup type does not implement <see cref="IStartup"/>.
/// </summary>
public class MissingStartupInterfaceException(Type startupType)
    : InvalidStartupTypeExceptionBase(startupType, $"implement {nameof(IStartup)}");
