using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting.Exceptions;

/// <summary>
/// Base for startup types that cannot be used to bootstrap a host.
/// </summary>
public abstract class InvalidStartupTypeExceptionBase(Type startupType, string requirement)
    : InvalidStateException($"Startup type '{startupType.FullName}' must {requirement}.")
{
    public Type StartupType { get; } = startupType;
}
