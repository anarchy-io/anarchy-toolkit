using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler.Exceptions;

/// <summary>
/// Thrown when an access token cannot be acquired from the identity provider.
/// </summary>
public class AccessTokenAcquisitionException(Exception innerException)
    : ExternalDependencyException($"Failed to get access token: {innerException.Message}", innerException);
