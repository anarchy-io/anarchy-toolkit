using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting.Exceptions;

/// <summary>
/// Base for startup types that cannot be used to bootstrap a host.
/// </summary>
/// <param name="startupType">The offending startup type.</param>
/// <param name="requirement">The unmet requirement, completing the sentence "Startup type '…' must …".</param>
public abstract class InvalidStartupTypeExceptionBase(Type startupType, string requirement)
    : InvalidStateException($"Startup type '{startupType.FullName}' must {requirement}.")
{
    /// <summary>
    /// Gets the offending startup type.
    /// </summary>
    public Type StartupType { get; } = startupType;
}
