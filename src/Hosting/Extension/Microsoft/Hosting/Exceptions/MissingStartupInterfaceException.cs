namespace Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting.Exceptions;

/// <summary>
/// Thrown when a startup type does not implement <see cref="IStartup"/>.
/// </summary>
/// <param name="startupType">The offending startup type.</param>
public class MissingStartupInterfaceException(Type startupType)
    : InvalidStartupTypeExceptionBase(startupType, $"implement {nameof(IStartup)}");
