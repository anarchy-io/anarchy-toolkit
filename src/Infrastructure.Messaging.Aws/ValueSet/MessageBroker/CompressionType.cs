using System.ComponentModel;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

/// <summary>
/// Compression algorithm applied to message bodies that exceed the AWS SNS/SQS size limit.
/// </summary>
/// <remarks>
/// The <see cref="DescriptionAttribute"/> value is the wire name written to the <c>CompressionType</c> message attribute
/// and parsed back by <see cref="Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker.InboundMessageMapper"/>.
/// </remarks>
public enum CompressionType
{
    /// <summary>GZip compression (<c>gzip</c>). The default for the AWS request builders.</summary>
    [Description("gzip")] Gzip,

    /// <summary>Zlib compression (<c>zlib</c>).</summary>
    [Description("zlib")] Zlib,

    /// <summary>No compression (<c>uncompressed</c>); oversized messages are rejected.</summary>
    [Description("uncompressed")] Uncompressed
}
