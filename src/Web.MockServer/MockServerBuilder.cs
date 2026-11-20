using Anarchy.Toolkit.Web.MockServer.Entity;
using Anarchy.Toolkit.Web.MockServer.Service;

namespace Anarchy.Toolkit.Web.MockServer;

/// <summary>
/// Customizes a mock server: consumer middlewares, expectations in code, services and options.
/// </summary>
/// <example>
/// <code language="csharp">
/// return await MockServerEntryPoint.RunAsync(args, server => server
///     .AddExpectationsFrom("mocks")
///     .Use(async (context, next) => {
///         if (context.Request.Headers.Authorization.Count == 0)
///         {
///             context.Response.StatusCode = 401;
///             context.GetMockServer().AnsweredBy = "auth middleware";
///             return;
///         }
///         await next(context);
///     }));
/// </code>
/// </example>
public sealed class MockServerBuilder
{
    internal MockServerBuilder(MockServerOptions options) => Options = options;

    /// <summary>The options, already read from the command line; changes here win over it.</summary>
    public MockServerOptions Options { get; }

    internal List<Action<IApplicationBuilder>> Middlewares { get; } = [];

    internal List<MockServerMiddleware> MiddlewareDescriptors { get; } = [];

    internal List<Action<WebApplicationBuilder>> HostConfigurations { get; } = [];

    internal List<Action<MockServerHost, WebApplication>> PipelineExtensions { get; } = [];

    internal List<Func<MockServerHost, string?>> UiUrls { get; } = [];

    internal List<Action<IServiceCollection>> Services { get; } = [];

    internal List<MockExpectation> Expectations { get; } = [];

    /// <summary>
    /// Adds a middleware, run after expectation matching (see <see cref="MockServerRequestFeature"/>) and before the
    /// matched expectation answers; it may answer by itself or change the match.
    /// </summary>
    /// <param name="middleware">The middleware.</param>
    /// <param name="name">Its name, shown in the UI and <c>/__mock/middlewares</c>; by default the delegate's method.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder Use(Func<HttpContext, RequestDelegate, Task> middleware, string? name = null) =>
        AddMiddleware(app => app.Use(middleware), name ?? DelegateName(middleware), "delegate");

    /// <summary>Adds a conventional middleware class (see <see cref="Use"/> for when it runs).</summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <param name="args">Constructor arguments not resolved from services.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder UseMiddleware<TMiddleware>(params object[] args) =>
        AddMiddleware(app => app.UseMiddleware<TMiddleware>(args), typeof(TMiddleware).Name, "middleware");

    /// <summary>Adds raw access to the application pipeline at the same point as <see cref="Use"/>.</summary>
    /// <param name="configure">Configures the pipeline.</param>
    /// <param name="name">Its name, shown in the UI and <c>/__mock/middlewares</c>; by default the delegate's method.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder Configure(Action<IApplicationBuilder> configure, string? name = null) =>
        AddMiddleware(configure, name ?? DelegateName(configure), "pipeline");

    /// <summary>
    /// Changes the underlying <see cref="WebApplicationBuilder"/> before it is built, e.g. to register a UI. Meant for
    /// extensions such as <c>Anarchy.Toolkit.Web.MockServer.Ui</c>.
    /// </summary>
    /// <param name="configure">Configures the builder.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder ConfigureWebApplication(Action<WebApplicationBuilder> configure)
    {
        HostConfigurations.Add(configure);
        return this;
    }

    /// <summary>
    /// Adds to the pipeline after the control API and before expectation matching, so what it maps is neither matched
    /// nor recorded as a mock request when it lives under <see cref="MockServerHost.ControlPath"/>. Meant for extensions
    /// such as <c>Anarchy.Toolkit.Web.MockServer.Ui</c>.
    /// </summary>
    /// <param name="configure">Configures the pipeline.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder ExtendPipeline(Action<MockServerHost, WebApplication> configure)
    {
        PipelineExtensions.Add(configure);
        return this;
    }

    /// <summary>Announces a UI in the startup banner.</summary>
    /// <param name="url">Gives the UI's URL once the server listens, or <see langword="null"/> when it is off.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder AnnounceUi(Func<MockServerHost, string?> url)
    {
        UiUrls.Add(url);
        return this;
    }

    private MockServerBuilder AddMiddleware(Action<IApplicationBuilder> configure, string name, string kind)
    {
        Middlewares.Add(configure);
        MiddlewareDescriptors.Add(new MockServerMiddleware(MiddlewareDescriptors.Count + 1, name, kind));
        return this;
    }

    private static string DelegateName(Delegate handler)
    {
        var method = handler.Method;
        return method.Name.Contains('<') || method.DeclaringType is null
            ? "anonymous"
            : $"{method.DeclaringType.Name}.{method.Name}";
    }

    /// <summary>Registers services, e.g. for middlewares.</summary>
    /// <param name="configure">Configures the services.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        Services.Add(configure);
        return this;
    }

    /// <summary>Changes the options.</summary>
    /// <param name="configure">Configures the options.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder ConfigureOptions(Action<MockServerOptions> configure)
    {
        configure(Options);
        return this;
    }

    /// <summary>Adds an expectation in code.</summary>
    /// <param name="expectation">The expectation.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder AddExpectation(MockExpectation expectation)
    {
        Expectations.Add(expectation);
        return this;
    }

    /// <summary>Adds a simple expectation in code.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path (exact, templated or regular expression).</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="body">The body: a string as-is, anything else serialized as JSON.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder AddExpectation(string method, string path, int statusCode = 200, object? body = null) =>
        AddExpectation(new MockExpectation {
            HttpRequest = new MockRequestMatcher { Method = method, Path = path },
            HttpResponse = new MockResponse {
                StatusCode = statusCode,
                Body = body is null ? null : System.Text.Json.JsonSerializer.SerializeToElement(body, MockServerJson.Options),
            },
        });

    /// <summary>Adds an expectation file, directory or glob pattern, in addition to those given on the command line.</summary>
    /// <param name="path">The file, directory or pattern.</param>
    /// <returns>This builder.</returns>
    public MockServerBuilder AddExpectationsFrom(string path)
    {
        Options.ExpectationPaths.Add(path);
        return this;
    }
}
