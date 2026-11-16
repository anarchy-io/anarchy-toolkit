using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.Exceptions;

/// <summary>
/// Thrown when <c>ThrowIfMissing</c> behaviour is enabled and the correlation headers are absent from the request.
/// </summary>
/// <remarks>
/// Raised by <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.CorrelationIdMiddleware"/>
/// when <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity.CorrelationIdOptions.Behavior"/> is
/// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity.CorrelationIdBehavior.ThrowIfMissing"/> and
/// either header is missing.
/// </remarks>
/// <param name="correlationIdHeaderName">The configured correlation id header name, included in the message.</param>
/// <param name="attemptIdHeaderName">The configured attempt id header name, included in the message.</param>
public class MissingCorrelationHeadersException(string correlationIdHeaderName, string attemptIdHeaderName)
    : InvalidStateException(
        $"ThrowIfMissing behavior is enabled. Ensure {correlationIdHeaderName} and {attemptIdHeaderName} are set in request headers.");
