using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Testing.Helpers.Tests;
using Anarchy.Toolkit.Testing.Telemetry;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Testing;

/// <summary>
/// Base class for toolkit-based test cases, providing a test host environment, an in-memory telemetry capture for
/// asserting on logs and metrics, and small helpers.
/// </summary>
/// <remarks>
/// A new <see cref="FixtureTelemetry"/> is created per test class instance (i.e. per test in xUnit).
/// Use <see cref="TestCaseBase{TAppSettings}"/> when test settings are needed.
/// </remarks>
public abstract class TestCaseBase
{
    /// <summary>
    /// Gets the test host environment (see <see cref="TestHelper.TestHostEnvironment"/>).
    /// </summary>
    /// <returns>An <see cref="IHostEnvironment"/> configured for tests.</returns>
    protected static IHostEnvironment TestEnvironment() => TestHelper.TestHostEnvironment();

    /// <summary>
    /// A <see cref="TestTelemetry"/> capturing logs and metrics in memory: pass its
    /// <see cref="TestTelemetry.Telemetry"/> to the code under test and assert on it afterwards.
    /// </summary>
    protected readonly TestTelemetry FixtureTelemetry = new();

    /// <summary>
    /// Returns the name of the calling member, useful to derive unique, test-specific names (e.g. queues or ids).
    /// </summary>
    /// <param name="caller">Filled in by the compiler via <see cref="CallerMemberNameAttribute"/>; do not pass explicitly.</param>
    /// <returns>The caller member name.</returns>
    protected static string? GetCallerName([CallerMemberName] string? caller = null)
    {
        return caller;
    }
}

/// <summary>
/// Test case exposing the test settings of the consumer's <typeparamref name="TAppSettings"/>, bound from the
/// <c>appsettings*.json</c> files embedded in the assembly declaring it.
/// </summary>
/// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
public abstract class TestCaseBase<TAppSettings> : TestCaseBase
    where TAppSettings : class, IAppSettings
{
    /// <summary>
    /// Gets the test settings bound to <typeparamref name="TAppSettings"/> (see <see cref="TestHelper.TestSettings{TAppSettings}"/>).
    /// </summary>
    /// <returns>The bound settings instance.</returns>
    protected static TAppSettings TestSettings() => TestHelper.TestSettings<TAppSettings>();
}
