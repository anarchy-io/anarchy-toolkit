namespace Anarchy.Toolkit.Runtime.Web.Composition;

/// <summary>
/// Non-generic contract of a web application startup, so hosts and entry points can be composed without knowing
/// the consumer's concrete application settings type.
/// </summary>
public interface IWebAppStartup : Anarchy.Toolkit.Runtime.IStartup
{
    /// <summary>Configures the HTTP request pipeline.</summary>
    /// <param name="app">The application builder.</param>
    void Configure(IApplicationBuilder app);
}
