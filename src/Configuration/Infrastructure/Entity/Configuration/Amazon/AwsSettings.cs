using Anarchy.Toolkit.Core.Extension;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Amazon;

public sealed record AwsSettings
{
    /// <summary>
    /// The AWS region identifier (for example <c>eu-west-1</c> or a region name such as <c>EuropeIreland</c>).
    /// Kept as a <see cref="string"/> so this package does not depend on a specific AWS region enum.
    /// </summary>
    [Required] public string RegionName { get; init; } = default!;
    [Required] public string ApiVersion { get; init; } = default!;
    [Required] public string AccountId { get; init; } = default!;
    public string? EndpointSqs { get; init; }
    public string? EndpointSns { get; init; }
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }

    public bool SqsUseCustomConnection()
    {
        return !(EndpointSqs.IsNullOrEmpty() || AccessKey.IsNullOrEmpty() || SecretKey.IsNullOrEmpty());
    }

    public bool SnsUseCustomConnection()
    {
        return !(EndpointSns.IsNullOrEmpty() || AccessKey.IsNullOrEmpty() || SecretKey.IsNullOrEmpty());
    }
}

public interface IAwsAppSettings : IAppSettings
{
    [Required] public AwsSettings Aws { get; init; }
}
