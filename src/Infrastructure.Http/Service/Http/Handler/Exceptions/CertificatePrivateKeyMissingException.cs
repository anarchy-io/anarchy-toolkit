using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler.Exceptions;

/// <summary>
/// Thrown when the client certificate used for MSAL authentication has no private key.
/// </summary>
public class CertificatePrivateKeyMissingException()
    : InvalidStateException("Certificate does not contain a private key, which is required for authentication.");
