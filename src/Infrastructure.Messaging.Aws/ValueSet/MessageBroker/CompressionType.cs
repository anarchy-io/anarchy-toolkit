using System.ComponentModel;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

public enum CompressionType
{
    [Description("gzip")] Gzip,
    [Description("zlib")] Zlib,
    [Description("uncompressed")] Uncompressed
}
