using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Testing.Helper.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Anarchy.Toolkit.Testing.Helper;

public static class TestHelper
{
    private static readonly Random RandomNumberGenerator = new();

    private const int IdDefaultMin = 1_000;
    private const int IdDefaultMax = 100_000;

    public static T RandomId<T>() where T : IBinaryNumber<T>
    {
        double range = IdDefaultMax - IdDefaultMin;
        var value = RandomNumberGenerator.NextDouble() * range;
        return T.CreateChecked(IdDefaultMin + value);
    }

    public static T RandomId<T>(T min, T? max = default) where T : IBinaryNumber<T>
    {
        var range = max is not null && max > min ? double.CreateChecked(max - min) : double.CreateChecked(min);
        var value = RandomNumberGenerator.NextDouble() * range;
        return min + T.CreateChecked(value);
    }

    public static T RandomNumber<T>(T max) where T : IBinaryNumber<T>
    {
        return T.CreateChecked(RandomNumberGenerator.NextDouble() * double.CreateChecked(0));
    }

    public static string RandomId(string prefix = "Test", string? suffix = null, int maxLength = 64)
    {
        prefix = prefix.IsNullOrEmpty() ? string.Empty : $"{prefix}.";
        suffix = suffix.IsNullOrEmpty() ? string.Empty : $".{suffix}";

        return $"{prefix}{Guid.NewGuid()}{suffix}".Left(maxLength)!;
    }

    public static byte[] RandomBytes()
    {
        return Encoding.ASCII.GetBytes(RandomId());
    }

    public static MemoryStream RandomMemoryStream()
    {
        return new MemoryStream(RandomBytes());
    }

    /// <summary>
    /// Returns the test settings bound from the <c>appsettings.json</c> and <c>appsettings.Development.json</c>
    /// files embedded in the assembly declaring <typeparamref name="TAppSettings"/>. The instance is built once per
    /// settings type and cached.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
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

    public static IHostEnvironment TestHostEnvironment()
    {
        return new HostingEnvironment {
            EnvironmentName = Environments.Development,
            ApplicationName = "Test",
            ContentRootPath = AppContext.BaseDirectory
        };
    }

    public static async Task WaitAsync(int seconds, string? reason = null,
                                       CancellationToken cancellationToken = default)
    {
        DevelopmentLogger.Print(
            $"Waiting for {seconds} seconds{(reason is not null ? $" because {reason}" : string.Empty)}...",
            "TestHelper",
            DevelopmentLoggerLevel.Info,
            null,
            false,
            DevelopmentLoggerLevel.Info.ToString()
        );

        while (seconds > 0)
        {
            await Task.Delay(1000, cancellationToken); // 1000ms = 1 second
            seconds--;
        }
    }

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

    public static bool RandomBool()
    {
        return RandomNumberGenerator.Next(0, 2) == 0;
    }

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

    public static DateTimeOffset LowPrecisionNow()
    {
        return DateTimeOffset.UtcNow.ToJson().FromJson<DateTimeOffset>();
    }
}
