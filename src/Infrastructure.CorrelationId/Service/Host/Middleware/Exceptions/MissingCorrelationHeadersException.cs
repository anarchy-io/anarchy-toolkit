using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.Exceptions;

/// <summary>
/// Thrown when <c>ThrowIfMissing</c> behaviour is enabled and the correlation headers are absent from the request.
/// </summary>
public class MissingCorrelationHeadersException(string correlationIdHeaderName, string attemptIdHeaderName)
    : InvalidStateException(
        $"ThrowIfMissing behavior is enabled. Ensure {correlationIdHeaderName} and {attemptIdHeaderName} are set in request headers.");
