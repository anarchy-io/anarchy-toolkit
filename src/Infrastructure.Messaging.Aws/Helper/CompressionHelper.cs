using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using System.IO.Compression;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper;

public static class CompressionHelper
{
    private static readonly UTF8Encoding StrictUtf8 =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string DecompressToUtf8(Stream input, CompressionType compressionType)
    {
        return compressionType switch {
            CompressionType.Gzip => DecompressUtf8With(
                stream => new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true),
                input),
            CompressionType.Zlib => DecompressUtf8With(
                stream => new ZLibStream(stream, CompressionMode.Decompress, leaveOpen: true),
                input),
            _ => ReadUtf8(input)
        };
    }

    public static byte[] CompressFromUtf8(string text, CompressionType compressionType,
                                          CompressionLevel level = CompressionLevel.Fastest)
    {
        var bytes = StrictUtf8.GetBytes(text);
        return compressionType switch {
            CompressionType.Gzip => CompressWith(ms => new GZipStream(ms, level, leaveOpen: true), bytes),
            CompressionType.Zlib => CompressWith(ms => new ZLibStream(ms, level, leaveOpen: true), bytes),
            _ => bytes
        };
    }

    private static string DecompressUtf8With(Func<Stream, Stream> wrap, Stream input)
    {
        using var wrapper = wrap(input);
        using var ms = new MemoryStream();
        wrapper.CopyTo(ms);
        return StrictUtf8.GetString(ms.ToArray());
    }

    private static string ReadUtf8(Stream input)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        return StrictUtf8.GetString(ms.ToArray());
    }

    private static byte[] CompressWith(Func<Stream, Stream> wrap, byte[] data)
    {
        using var ms = new MemoryStream();
        using (var wrapper = wrap(ms))
        {
            wrapper.Write(data, 0, data.Length);
        }

        return ms.ToArray();
    }
}
