using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Testing.Helper.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Anarchy.Toolkit.Testing.Helper;

/// <summary>
/// General-purpose helpers for tests: random data generation, test settings and host environment, waiting,
/// and reflection-based property overriding.
/// </summary>
/// <remarks>
/// Random values come from a single shared <see cref="Random"/> instance and are not cryptographically secure
/// nor reproducible across runs.
/// </remarks>
public static class TestHelper
{
    private static readonly Random RandomNumberGenerator = new();

    private const int IdDefaultMin = 1_000;
    private const int IdDefaultMax = 100_000;

    /// <summary>
    /// Returns a random numeric id in the range [1,000, 100,000).
    /// </summary>
    /// <typeparam name="T">The numeric type of the id (e.g. <see cref="int"/> or <see cref="long"/>).</typeparam>
    /// <returns>A random id.</returns>
    public static T RandomId<T>() where T : IBinaryNumber<T>
    {
        double range = IdDefaultMax - IdDefaultMin;
        var value = RandomNumberGenerator.NextDouble() * range;
        return T.CreateChecked(IdDefaultMin + value);
    }

    /// <summary>
    /// Returns a random numeric id greater than or equal to <paramref name="min"/>.
    /// </summary>
    /// <remarks>
    /// When <paramref name="max"/> is greater than <paramref name="min"/>, the value is in [<paramref name="min"/>,
    /// <paramref name="max"/>); otherwise it is in [<paramref name="min"/>, 2 × <paramref name="min"/>).
    /// </remarks>
    /// <typeparam name="T">The numeric type of the id.</typeparam>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound; ignored unless greater than <paramref name="min"/>.</param>
    /// <returns>A random id.</returns>
    public static T RandomId<T>(T min, T? max = default) where T : IBinaryNumber<T>
    {
        var range = max is not null && max > min ? double.CreateChecked(max - min) : double.CreateChecked(min);
        var value = RandomNumberGenerator.NextDouble() * range;
        return min + T.CreateChecked(value);
    }

    /// <summary>
    /// Intended to return a random number up to <paramref name="max"/>.
    /// </summary>
    /// <remarks>
    /// Note: the current implementation ignores <paramref name="max"/> and always returns zero.
    /// Prefer <see cref="RandomId{T}(T, T)"/>.
    /// </remarks>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="max">The intended upper bound (currently unused).</param>
    /// <returns>Currently always zero.</returns>
    public static T RandomNumber<T>(T max) where T : IBinaryNumber<T>
    {
        return T.CreateChecked(RandomNumberGenerator.NextDouble() * double.CreateChecked(0));
    }

    /// <summary>
    /// Returns a random string id in the form <c>{prefix}.{guid}.{suffix}</c>, truncated to <paramref name="maxLength"/> characters.
    /// </summary>
    /// <param name="prefix">Prefix to prepend; omitted (with its dot) when <see langword="null"/> or empty.</param>
    /// <param name="suffix">Suffix to append; omitted (with its dot) when <see langword="null"/> or empty.</param>
    /// <param name="maxLength">Maximum length of the result.</param>
    /// <returns>A random string id.</returns>
    public static string RandomId(string prefix = "Test", string? suffix = null, int maxLength = 64)
    {
        prefix = prefix.IsNullOrEmpty() ? string.Empty : $"{prefix}.";
        suffix = suffix.IsNullOrEmpty() ? string.Empty : $".{suffix}";

        return $"{prefix}{Guid.NewGuid()}{suffix}".Left(maxLength)!;
    }

    /// <summary>
    /// Returns the ASCII bytes of a random string id (see <see cref="RandomId(string, string, int)"/>).
    /// </summary>
    /// <returns>A random byte array.</returns>
    public static byte[] RandomBytes()
    {
        return Encoding.ASCII.GetBytes(RandomId());
    }

    /// <summary>
    /// Returns a <see cref="MemoryStream"/> over <see cref="RandomBytes"/>.
    /// </summary>
    /// <returns>A new stream positioned at the start.</returns>
    public static MemoryStream RandomMemoryStream()
    {
        return new MemoryStream(RandomBytes());
    }

    /// <summary>
    /// Returns the test settings bound from the <c>appsettings.json</c> and <c>appsettings.Development.json</c>
    /// files embedded in the assembly declaring <typeparamref name="TAppSettings"/>. The instance is built once per
    /// settings type and cached.
    /// </summary>
    /// <remarks>
    /// Resources are located by name suffix; when not found, <c>{AssemblyName}.{fileName}</c> is assumed. Remember to
    /// mark the files as <c>EmbeddedResource</c> in the test project.
    /// </remarks>
    /// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
    /// <returns>The cached settings instance.</returns>
    public static TAppSettings TestSettings<TAppSettings>() where TAppSettings : class, IAppSettings
    {
        return TestSettingsCache<TAppSettings>.Instance;
    }

    private static class TestSettingsCache<TAppSettings> where TAppSettings : class, IAppSettings
    {
        public static readonly TAppSettings Instance = new ConfigurationBuilder()
            .AddEmbeddedJson(
                typeof(TAppSettings).Assembly,
                ResolveEmbeddedSettingsResource(typeof(TAppSettings).Assembly, "appsettings.json"),
                ResolveEmbeddedSettingsResource(typeof(TAppSettings).Assembly, "appsettings.Development.json"))
            .Build()
            .Get<TAppSettings>()!;
    }

    private static string ResolveEmbeddedSettingsResource(Assembly assembly, string fileName)
    {
        return assembly
                   .GetManifestResourceNames()
                   .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
               ?? $"{assembly.GetName().Name}.{fileName}";
    }

    /// <summary>
    /// Creates an <see cref="IHostEnvironment"/> for tests: environment <c>Development</c>, application name <c>Test</c>,
    /// and content root <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    /// <returns>A new host environment.</returns>
    public static IHostEnvironment TestHostEnvironment()
    {
        return new HostingEnvironment {
            EnvironmentName = Environments.Development,
            ApplicationName = "Test",
            ContentRootPath = AppContext.BaseDirectory
        };
    }

    /// <summary>
    /// Waits for the given number of seconds, logging the optional reason first.
    /// </summary>
    /// <remarks>Useful when waiting for eventually consistent resources (e.g. queues). Avoid where polling is possible.</remarks>
    /// <param name="seconds">Number of seconds to wait.</param>
    /// <param name="reason">Optional reason included in the log line.</param>
    /// <param name="cancellationToken">Token used to cancel the wait.</param>
    /// <param name="telemetry">Receives the log line; nothing is logged when <see langword="null"/>.</param>
    /// <returns>A task that completes after the delay.</returns>
    public static async Task WaitAsync(int seconds, string? reason = null,
                                       CancellationToken cancellationToken = default, AppTelemetry? telemetry = null)
    {
        telemetry?.LogInformation(new CallSite(nameof(TestHelper), nameof(WaitAsync)),
            "Waiting for {Seconds} seconds{Reason}...", seconds, reason is not null ? $" because {reason}" : string.Empty);

        while (seconds > 0)
        {
            await Task.Delay(1000, cancellationToken); // 1000ms = 1 second
            seconds--;
        }
    }

    /// <summary>
    /// Sets a public or non-public instance property on <paramref name="target"/> via reflection, e.g. to force
    /// otherwise read-only state in tests.
    /// </summary>
    /// <typeparam name="T">The type declaring the property (the compile-time type is used for lookup).</typeparam>
    /// <param name="target">The object to modify.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="newValue">The value to assign.</param>
    /// <exception cref="PropertyNotFoundException">No instance property named <paramref name="propertyName"/> exists on <typeparamref name="T"/>.</exception>
    public static void OverrideProperty<T>(
        T target,
        string propertyName,
        object? newValue)
    {
        var prop = typeof(T).GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (prop is null)
            throw new PropertyNotFoundException(typeof(T), propertyName);

        prop.SetValue(target, newValue);
    }

    /// <summary>
    /// Returns a random value of <typeparamref name="TEnum"/>, optionally excluding some values.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="exclude">Values that must not be returned, or <see langword="null"/>.</param>
    /// <returns>A random enum value.</returns>
    /// <exception cref="NoEnumValuesAvailableException">Every value was excluded.</exception>
    public static TEnum RandomEnum<TEnum>(IReadOnlyCollection<TEnum>? exclude = null)
        where TEnum : struct, Enum
    {
        var values = Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Where(v => exclude == null || !exclude.Contains(v))
            .ToArray();

        if (values.Length == 0)
            throw new NoEnumValuesAvailableException(typeof(TEnum));

        return values[RandomNumberGenerator.Next(values.Length)];
    }

    /// <summary>Returns a random boolean.</summary>
    /// <returns><see langword="true"/> or <see langword="false"/> with equal probability.</returns>
    public static bool RandomBool()
    {
        return RandomNumberGenerator.Next(0, 2) == 0;
    }

    /// <summary>
    /// Returns an anonymous object with random string, integer, long and boolean properties and arrays thereof,
    /// useful as an arbitrary payload.
    /// </summary>
    /// <returns>A new anonymous object.</returns>
    public static object RandomObject()
    {
        return new {
            RandomString = RandomId(),
            RandomInteger = RandomId<int>(),
            RandomLong = RandomId<long>(),
            RandomBoolean = RandomBool(),
            RandomArrayString = new[] { RandomId(), RandomId() },
            RandomArrayInteger = new[] { RandomId<int>(), RandomId<int>() },
            RandomArrayLong = new[] { RandomId<long>(), RandomId<long>() },
            RandomArrayBoolean = new[] { RandomBool(), RandomBool() },
        };
    }

    /// <summary>
    /// Returns the current UTC time after a JSON round-trip, so it compares equal to values that went through
    /// JSON serialization (avoiding precision mismatches).
    /// </summary>
    /// <returns>The current time with JSON precision.</returns>
    public static DateTimeOffset LowPrecisionNow()
    {
        return DateTimeOffset.UtcNow.ToJson().FromJson<DateTimeOffset>();
    }
}
