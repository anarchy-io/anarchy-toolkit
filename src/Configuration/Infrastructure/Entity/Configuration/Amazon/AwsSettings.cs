using Anarchy.Toolkit.Core.Extension;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Amazon;

/// <summary>
/// AWS connection settings, bound from the <c>Aws</c> configuration section (see <see cref="IAwsAppSettings"/>).
/// </summary>
/// <remarks>
/// The optional endpoint and key properties allow pointing SQS/SNS clients at a custom endpoint (e.g. LocalStack) with
/// explicit credentials; see <see cref="SqsUseCustomConnection"/> and <see cref="SnsUseCustomConnection"/>.
/// </remarks>
public sealed record AwsSettings
{
    /// <summary>
    /// The AWS region identifier (for example <c>eu-west-1</c> or a region name such as <c>EuropeIreland</c>).
    /// Kept as a <see cref="string"/> so this package does not depend on a specific AWS region enum.
    /// </summary>
    [Required] public string RegionName { get; init; } = default!;

    /// <summary>The AWS API version. Required.</summary>
    [Required] public string ApiVersion { get; init; } = default!;

    /// <summary>The AWS account id. Required.</summary>
    [Required] public string AccountId { get; init; } = default!;

    /// <summary>Optional custom SQS service endpoint.</summary>
    public string? EndpointSqs { get; init; }

    /// <summary>Optional custom SNS service endpoint.</summary>
    public string? EndpointSns { get; init; }

    /// <summary>Optional access key used with custom endpoints.</summary>
    public string? AccessKey { get; init; }

    /// <summary>Optional secret key used with custom endpoints.</summary>
    public string? SecretKey { get; init; }

    /// <summary>
    /// Determines whether SQS clients should use a custom connection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when <see cref="EndpointSqs"/>, <see cref="AccessKey"/> and <see cref="SecretKey"/> are
    /// all set.
    /// </returns>
    public bool SqsUseCustomConnection()
    {
        return !(EndpointSqs.IsNullOrEmpty() || AccessKey.IsNullOrEmpty() || SecretKey.IsNullOrEmpty());
    }

    /// <summary>
    /// Determines whether SNS clients should use a custom connection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when <see cref="EndpointSns"/>, <see cref="AccessKey"/> and <see cref="SecretKey"/> are
    /// all set.
    /// </returns>
    public bool SnsUseCustomConnection()
    {
        return !(EndpointSns.IsNullOrEmpty() || AccessKey.IsNullOrEmpty() || SecretKey.IsNullOrEmpty());
    }
}

/// <summary>
/// Capability interface for application settings that contain an <c>Aws</c> section.
/// </summary>
public interface IAwsAppSettings : IAppSettings
{
    /// <summary>The AWS settings (configuration section <c>Aws</c>). Required.</summary>
    [Required] public AwsSettings Aws { get; init; }
}
