using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using System.IO.Compression;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.Compression;

/// <summary>
/// Compresses and decompresses UTF-8 text with the algorithms of <see cref="CompressionType"/>.
/// </summary>
/// <remarks>
/// Uses strict UTF-8 (no BOM, throws on invalid bytes). Used by the AWS request builders to compress oversized
/// messages and by the inbound mapper to restore them.
/// </remarks>
public static class CompressionHelper
{
    private static readonly UTF8Encoding StrictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Decompresses <paramref name="input"/> and decodes it as UTF-8.
    /// </summary>
    /// <param name="input">The compressed stream. It is read to the end but not disposed.</param>
    /// <param name="compressionType">The algorithm used; <see cref="CompressionType.Uncompressed"/> reads the stream as is.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="DecoderFallbackException">The decompressed bytes are not valid UTF-8.</exception>
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

    /// <summary>
    /// Encodes <paramref name="text"/> as UTF-8 and compresses it.
    /// </summary>
    /// <param name="text">The text to compress.</param>
    /// <param name="compressionType">The algorithm; <see cref="CompressionType.Uncompressed"/> returns the raw UTF-8 bytes.</param>
    /// <param name="level">The compression level. Defaults to <see cref="CompressionLevel.Fastest"/>.</param>
    /// <returns>The compressed bytes.</returns>
    /// <exception cref="EncoderFallbackException"><paramref name="text"/> contains invalid UTF-16 (e.g. lone surrogates).</exception>
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
