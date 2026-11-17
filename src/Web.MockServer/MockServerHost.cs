using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Web.MockServer.Entity;
using Anarchy.Toolkit.Web.MockServer.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Anarchy.Toolkit.Web.MockServer;

/// <summary>
/// A running mock server. Start it from a process of your own (or a test) with <see cref="StartAsync"/>, or run it as a
/// console application with <see cref="MockServerEntryPoint"/>.
/// </summary>
/// <remarks>
/// Besides the expectations, it serves a control API under <c>/__mock</c>:
/// <list type="bullet">
/// <item><c>GET /__mock/health</c>: <c>200</c> once ready.</item>
/// <item><c>GET /__mock/requests</c> (optional <c>method</c> and <c>path</c> regular-expression filters) and <c>DELETE /__mock/requests</c>.</item>
/// <item><c>GET /__mock/expectations</c>, <c>PUT /__mock/expectations</c> (one or an array, added at runtime) and
/// <c>DELETE /__mock/expectations[?id=…]</c> (removes runtime ones).</item>
/// <item><c>POST /__mock/reset</c>: forgets requests and runtime expectations; <c>POST /__mock/reload</c>: reloads files.</item>
/// </list>
/// </remarks>
public sealed class MockServerHost : IAsyncDisposable
{
    /// <summary>The path prefix of the control API.</summary>
    public const string ControlPath = "/__mock";

    private readonly WebApplication _app;
    private readonly MockServerBuilder _builder;
    private readonly List<FileSystemWatcher> _watchers = [];
    private CancellationTokenSource? _reloadDebounce;

    private MockServerHost(WebApplication app, MockServerBuilder builder, MockServerConsole console, ExpectationStore store, RequestRecorder recorder)
    {
        _app = app;
        _builder = builder;
        Console = console;
        Expectations = store;
        Recorder = recorder;
    }

    /// <summary>The options in use.</summary>
    public MockServerOptions Options => _builder.Options;

    /// <summary>The port listened on.</summary>
    public int Port { get; private set; }

    /// <summary>The base address, e.g. <c>http://localhost:6000</c>.</summary>
    public Uri BaseAddress { get; private set; } = null!;

    /// <summary>The expectations answered with.</summary>
    public ExpectationStore Expectations { get; }

    /// <summary>The received requests.</summary>
    public RequestRecorder Recorder { get; }

    /// <summary>The console output.</summary>
    public MockServerConsole Console { get; }

    /// <summary>The underlying ASP.NET Core application.</summary>
    public WebApplication App => _app;

    /// <summary>
    /// Builds and starts a mock server. Without a port it uses <see cref="MockServerOptions.DefaultPort"/>, or the next
    /// free one (port <c>0</c> lets the operating system choose).
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="configure">Customizes the server.</param>
    /// <param name="cancellationToken">Cancels the startup.</param>
    /// <returns>The running server.</returns>
    /// <exception cref="InvalidOperationException">The requested port is taken.</exception>
    public static async Task<MockServerHost> StartAsync(
        MockServerOptions? options = null, Action<MockServerBuilder>? configure = null, CancellationToken cancellationToken = default)
    {
        var builder = new MockServerBuilder(options ?? new MockServerOptions());
        configure?.Invoke(builder);
        options = builder.Options;

        var console = new MockServerConsole(options);
        var port = options.Port switch {
            null => LocalPorts.FindAvailable(MockServerOptions.DefaultPort),
            0 => LocalPorts.FindAvailable(0, maxAttempts: 0),
            { } explicitPort when LocalPorts.IsAvailable(explicitPort) => explicitPort,
            { } explicitPort => throw new InvalidOperationException($"Port {explicitPort} is already in use."),
        };
        if (options.Port is null && port != MockServerOptions.DefaultPort)
            console.Warning($"Port {MockServerOptions.DefaultPort} is in use, using {port} instead.");

        var store = new ExpectationStore();
        var recorder = new RequestRecorder(options);

        var webBuilder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [], ContentRootPath = Directory.GetCurrentDirectory() });
        webBuilder.Logging.ClearProviders();
        if (console.IsEnabled(MockServerVerbosity.Diagnostic)) webBuilder.Logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Information);
        webBuilder.WebHost.UseUrls($"http://{options.Host}:{port}");
        webBuilder.Services.AddSingleton(options).AddSingleton(store).AddSingleton(recorder).AddSingleton(console);
        foreach (var services in builder.Services) services(webBuilder.Services);

        var app = webBuilder.Build();
        var host = new MockServerHost(app, builder, console, store, recorder);
        var load = host.LoadExpectations();
        host.ConfigurePipeline();

        await app.StartAsync(cancellationToken);

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        host.BaseAddress = new Uri(address.Replace("[::]", "localhost").Replace("0.0.0.0", "localhost").Replace("127.0.0.1", "localhost"));
        host.Port = host.BaseAddress.Port;

        console.Banner(host.BaseAddress.ToString().TrimEnd('/'), options, load, builder.Expectations.Count, builder.Middlewares.Count, recorder.File);
        if (options.WatchExpectations) host.Watch();
        if (options.KillTimeout is { } timeout) host.ScheduleKill(timeout);

        return host;
    }

    /// <summary>Waits until the server stops (Ctrl+C, SIGTERM, the kill timeout or <see cref="StopAsync"/>).</summary>
    /// <returns>A task completing on shutdown.</returns>
    public Task WaitForShutdownAsync() => _app.WaitForShutdownAsync();

    /// <summary>Stops the server.</summary>
    /// <returns>A task completing once stopped.</returns>
    public Task StopAsync() => _app.StopAsync();

    /// <summary>Reloads the expectation files and code expectations, keeping runtime ones.</summary>
    /// <returns>What was loaded.</returns>
    public ExpectationLoader.Result LoadExpectations()
    {
        var load = ExpectationLoader.Load(Options.ExpectationPaths);
        Expectations.ReplaceLoaded(_builder.Expectations.Concat(load.Expectations));
        return load;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var watcher in _watchers) watcher.Dispose();
        await _app.DisposeAsync();
    }

    private void ConfigurePipeline()
    {
        _app.Use(RecordAsync);
        _app.Map(ControlPath, control => control.Run(HandleControlAsync));
        _app.Use((context, next) => {
            var feature = context.GetMockServer();
            feature.Expectation = Expectations.FindMatch(feature.Request);
            return next(context);
        });
        foreach (var middleware in _builder.Middlewares) middleware(_app);
        _app.Run(RespondAsync);
    }

    private async Task RecordAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTimeOffset.Now;
        var control = context.Request.Path.StartsWithSegments(ControlPath);

        context.Request.EnableBuffering();
        string? body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true)) body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;
        if (body.Length == 0) body = null;

        var request = MockRequest.From(context.Request, body);
        var feature = new MockServerRequestFeature(request);
        context.Features.Set(feature);

        var captureResponse = Console.IsEnabled(MockServerVerbosity.Diagnostic);
        var originalBody = context.Response.Body;
        using var captured = captureResponse ? new MemoryStream() : null;
        if (captured is not null) context.Response.Body = captured;

        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            Console.Error($"{request.Method} {request.Path} failed: {exception.Message}");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message }, MockServerJson.Options);
        }
        finally
        {
            if (captured is not null)
            {
                captured.Position = 0;
                await captured.CopyToAsync(originalBody);
                context.Response.Body = originalBody;
            }
        }

        var record = new RecordedRequest {
            Timestamp = timestamp,
            Method = request.Method,
            Path = request.Path,
            QueryString = context.Request.QueryString.Value ?? "",
            Query = request.Query.ToDictionary(),
            Headers = request.Headers.ToDictionary(),
            Body = body,
            MatchedExpectation = feature.AnsweredBy,
            StatusCode = context.Response.StatusCode,
            DurationMs = stopwatch.Elapsed.TotalMilliseconds,
        };
        if (!control) Recorder.Record(record);
        Console.Request(record, control, captured is null ? null : Encoding.UTF8.GetString(captured.ToArray()));
    }

    private async Task RespondAsync(HttpContext context)
    {
        var feature = context.GetMockServer();
        if (context.Response.HasStarted) return;

        if (feature.Expectation is not { } expectation)
        {
            context.Response.StatusCode = Options.UnmatchedStatusCode;
            await context.Response.WriteAsJsonAsync(new {
                error = "No expectation matches this request.",
                method = feature.Request.Method,
                path = feature.Request.Path,
                samePath = Expectations.All
                    .Where(e => ExpectationMatcher.MatchPath(e.HttpRequest.Path, feature.Request.Path) != ExpectationMatcher.PathMatch.None)
                    .Select(e => e.DisplayName),
            }, MockServerJson.Options);
            return;
        }

        feature.AnsweredBy ??= expectation.DisplayName;
        var response = expectation.HttpResponse;
        if (DelayOf(response.Delay) is { } delay && delay > TimeSpan.Zero) await Task.Delay(delay, context.RequestAborted);

        context.Response.StatusCode = response.StatusCode;
        foreach (var (name, value) in response.Headers ?? [])
            context.Response.Headers[name] = ExpectationMatcher.ValuesOf(value).ToArray();

        var (text, isJson) = BodyOf(response.Body);
        if (text is null) return;
        if (isJson && !context.Response.Headers.ContainsKey("Content-Type")) context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(text, context.RequestAborted);
    }

    private static (string? Text, bool IsJson) BodyOf(JsonElement? body)
    {
        if (body is not { } element || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return (null, false);
        if (element.ValueKind == JsonValueKind.String) return (element.GetString(), false);
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
        {
            switch (type.GetString()?.ToUpperInvariant())
            {
                case "JSON" when element.TryGetProperty("json", out var json):
                    return (json.ValueKind == JsonValueKind.String ? json.GetString() : json.GetRawText(), true);
                case "STRING" when element.TryGetProperty("string", out var text):
                    return (text.GetString(), false);
            }
        }
        return (element.GetRawText(), true);
    }

    internal static TimeSpan? DelayOf(JsonElement? delay)
    {
        if (delay is not { } element) return null;
        if (element.ValueKind == JsonValueKind.Number) return TimeSpan.FromMilliseconds(element.GetDouble());
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty("value", out var value)) return null;
        var unit = element.TryGetProperty("timeUnit", out var u) ? u.GetString()?.ToUpperInvariant() : "MILLISECONDS";
        return unit switch {
            "SECONDS" => TimeSpan.FromSeconds(value.GetDouble()),
            "MINUTES" => TimeSpan.FromMinutes(value.GetDouble()),
            "MICROSECONDS" => TimeSpan.FromMicroseconds(value.GetDouble()),
            "NANOSECONDS" => TimeSpan.FromTicks((long)(value.GetDouble() / 100)),
            _ => TimeSpan.FromMilliseconds(value.GetDouble()),
        };
    }

    private async Task HandleControlAsync(HttpContext context)
    {
        var request = context.GetMockServer().Request;
        var path = context.Request.Path.Value?.Trim('/') ?? "";
        var method = context.Request.Method.ToUpperInvariant();

        switch (path, method)
        {
            case ("health", "GET"):
                await context.Response.WriteAsJsonAsync(new { status = "ok", port = Port }, MockServerJson.Options);
                break;
            case ("requests", "GET"):
                var methodFilter = context.Request.Query["method"].ToString();
                var pathFilter = context.Request.Query["path"].ToString();
                var requests = Recorder.All.Where(r =>
                    (methodFilter.Length == 0 || string.Equals(r.Method, methodFilter, StringComparison.OrdinalIgnoreCase))
                    && (pathFilter.Length == 0 || ExpectationMatcher.MatchPath(pathFilter, r.Path) != ExpectationMatcher.PathMatch.None));
                await context.Response.WriteAsJsonAsync(requests, MockServerJson.Options);
                break;
            case ("requests", "DELETE"):
                await context.Response.WriteAsJsonAsync(new { cleared = Recorder.Clear() }, MockServerJson.Options);
                break;
            case ("expectations", "GET"):
                await context.Response.WriteAsJsonAsync(Expectations.All, MockServerJson.Options);
                break;
            case ("expectations", "PUT" or "POST"):
                try
                {
                    var added = ExpectationLoader.Parse(request.Body ?? "", "runtime");
                    Expectations.AddRuntime(added);
                    Console.Info($"Added {added.Count} runtime expectation(s).");
                    context.Response.StatusCode = StatusCodes.Status201Created;
                    await context.Response.WriteAsJsonAsync(new { added = added.Count }, MockServerJson.Options);
                }
                catch (Exception exception) when (exception is JsonException or InvalidDataException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = exception.Message }, MockServerJson.Options);
                }
                break;
            case ("expectations", "DELETE"):
                var id = context.Request.Query["id"].ToString();
                await context.Response.WriteAsJsonAsync(new { removed = Expectations.ClearRuntime(id.Length == 0 ? null : id) }, MockServerJson.Options);
                break;
            case ("reset", "POST"):
                var removed = Expectations.ClearRuntime();
                var cleared = Recorder.Clear();
                await context.Response.WriteAsJsonAsync(new { removed, cleared }, MockServerJson.Options);
                break;
            case ("reload", "POST"):
                var load = Reload();
                await context.Response.WriteAsJsonAsync(new { loaded = load.Expectations.Count, errors = load.Errors.Select(e => $"{e.Source}: {e.Error}") }, MockServerJson.Options);
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = $"Unknown control endpoint {method} {ControlPath}/{path}" }, MockServerJson.Options);
                break;
        }
    }

    private ExpectationLoader.Result Reload()
    {
        var load = LoadExpectations();
        Console.Info($"Reloaded {load.Expectations.Count} expectation(s) from {load.Files.Count} file(s).");
        foreach (var (source, error) in load.Errors) Console.Warning($"{source}: {error}");
        return load;
    }

    private void Watch()
    {
        var directories = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in Options.ExpectationPaths)
        {
            var full = Path.GetFullPath(source);
            var directory = Directory.Exists(full) ? full : Path.GetDirectoryName(full);
            while (directory is not null && !Directory.Exists(directory)) directory = Path.GetDirectoryName(directory);
            if (directory is not null && !directories.Any(d => directory.StartsWith(d + Path.DirectorySeparatorChar) || directory == d))
                directories.Add(directory);
        }

        foreach (var directory in directories)
        {
            var watcher = new FileSystemWatcher(directory, "*.json") { IncludeSubdirectories = true, EnableRaisingEvents = true };
            watcher.Changed += (_, _) => ScheduleReload();
            watcher.Created += (_, _) => ScheduleReload();
            watcher.Deleted += (_, _) => ScheduleReload();
            watcher.Renamed += (_, _) => ScheduleReload();
            _watchers.Add(watcher);
        }
    }

    private void ScheduleReload()
    {
        var debounce = new CancellationTokenSource();
        Interlocked.Exchange(ref _reloadDebounce, debounce)?.Cancel();
        _ = Task.Delay(TimeSpan.FromMilliseconds(300), debounce.Token).ContinueWith(task => {
            if (!task.IsCanceled) Reload();
        }, TaskScheduler.Default);
    }

    private void ScheduleKill(TimeSpan timeout)
    {
        var lifetime = _app.Services.GetRequiredService<IHostApplicationLifetime>();
        _ = Task.Delay(timeout, lifetime.ApplicationStopping).ContinueWith(task => {
            if (task.IsCanceled) return;
            Console.Warning($"Kill timeout of {timeout.TotalSeconds:0}s reached, stopping.");
            lifetime.StopApplication();
        }, TaskScheduler.Default);
    }
}
