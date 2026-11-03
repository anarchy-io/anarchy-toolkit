using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost.Exceptions;

/// <summary>
/// Thrown when the self-hosted health API is started more than once.
/// </summary>
public class HealthApiAlreadyStartedException()
    : InvalidStateException("The web host has already started. You can only start it once.");
