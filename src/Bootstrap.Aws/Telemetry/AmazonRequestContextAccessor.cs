using Anarchy.Toolkit.Bootstrap.Aws.Core.Service;

namespace Anarchy.Toolkit.Bootstrap.Aws.Telemetry;

/// <summary>
/// Default <see cref="IAmazonRequestContext"/> reading from <see cref="AmazonRequestContext"/>.
/// </summary>
/// <remarks>
/// Stateless: values are read from async-local storage on each access, so a singleton instance is safe.
/// </remarks>
public sealed class AmazonRequestContextAccessor : IAmazonRequestContext
{
    /// <inheritdoc />
    public string AmazonTraceId {
        get => AmazonRequestContext.AmazonTraceId;
    }
}
