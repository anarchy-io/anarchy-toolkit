using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Runtime.Host.Exceptions;

/// <summary>
/// Thrown when the application name cannot be derived from the entry point assembly name.
/// </summary>
/// <param name="entryPointType">The entry point type whose assembly name could not be resolved.</param>
public class AppNameNotResolvedException(Type entryPointType)
    : InvalidStateException($"AppName not specified and cannot be derived from {entryPointType.Assembly.GetName()}");
