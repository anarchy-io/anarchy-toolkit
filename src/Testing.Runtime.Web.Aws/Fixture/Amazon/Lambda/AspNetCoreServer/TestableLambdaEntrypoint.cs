using Anarchy.Toolkit.Runtime.Web.Aws;
using Anarchy.Toolkit.Runtime.Web.Composition;

namespace Anarchy.Toolkit.Testing.Runtime.Web.Aws.Fixture.Amazon.Lambda.AspNetCoreServer;

/// <summary>
/// Test-oriented AWS web-app entrypoint that forces the ASP.NET Core host into the
/// <see cref="Environments.Development"/> environment so end-to-end tests exercise the
/// Lambda pipeline with development configuration.
/// </summary>
/// <typeparam name="T">The application startup type.</typeparam>
public class TestableLambdaEntrypoint<T> : AwsWebAppEntryPointBase<T> where T : class, IWebAppStartup
{
    /// <inheritdoc />
    protected override void Init(IHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
    }
}
