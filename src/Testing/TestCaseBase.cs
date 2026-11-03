using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Testing.Helper;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Testing;

public abstract class TestCaseBase
{
    protected static IHostEnvironment TestEnvironment() => TestHelper.TestHostEnvironment();

    protected readonly InMemoryObserver FixtureObserver = InMemoryObserver.Create();

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
    protected static TAppSettings TestSettings() => TestHelper.TestSettings<TAppSettings>();
}
