using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Anarchy.Toolkit.Core.Helper;

public class HashHelper
{
    /// <summary>
    /// Hashes the given object using SHA1 algorithm
    /// </summary>
    /// <param name="input">The object to hash</param>
    /// <returns>The SHA1 hash as a hexadecimal string</returns>
    public static string HashWithSha1(object input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input), "Input cannot be null");
        }

        var inputBytes = input switch {
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            _ => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))
        };

        var hashBytes = SHA1.HashData(inputBytes);

        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Hashes the given object using MD5 algorithm
    /// </summary>
    /// <param name="input">The object to hash</param>
    /// <returns>The MD5 hash as a hexadecimal string</returns>
    public static string HashWithMd5(object input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input), "Input cannot be null");
        }

        var inputBytes = input switch {
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            _ => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))
        };

        var hashBytes = MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes);
    }
}
