namespace Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting.Exceptions;

/// <summary>
/// Thrown when a startup type has no <c>(IHostEnvironment, IConfiguration)</c> constructor.
/// </summary>
/// <param name="startupType">The offending startup type.</param>
public class MissingStartupConstructorException(Type startupType)
    : InvalidStartupTypeExceptionBase(startupType, "define a constructor (IHostEnvironment, IConfiguration)");
