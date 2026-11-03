using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Hosting.Host.Exceptions;

/// <summary>
/// Thrown when the application name cannot be derived from the entry point assembly name.
/// </summary>
public class AppNameNotResolvedException(Type entryPointType)
    : InvalidStateException($"AppName not specified and cannot be derived from {entryPointType.Assembly.GetName()}");
