using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Service.Builder;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.CorrelationId.Service.Host.Middleware;

public class MiddlewareTests : IDisposable
{
    private IHost _host = default!;
    private TestServer _testServer = default!;

    private void CreateTestServer(CorrelationIdOptions? options = null, ILoggerProvider? loggerProvider = null,
                                  RequestDelegate? next = null)
    {
        _host = new HostBuilder()
           .ConfigureWebHost(webBuilder => webBuilder
               .UseTestServer()
               .ConfigureServices(services => {
                   services.AddCorrelationId(options ?? new CorrelationIdOptions());
                   if (loggerProvider is not null)
                       services.AddLogging(logging => logging.AddProvider(loggerProvider));
               })
               .Configure(app => {
                   app.UseCorrelationId();
                   if (next is not null)
                       app.Run(next);
               }))
           .Start();

        _testServer = _host.GetTestServer();
    }

    [Fact]
    public async Task CorrelationMiddleware_GeneratesHeadersAsDefault()
    {
        CreateTestServer();

        var response =
            await _testServer.CreateRequest("/").GetAsync();

        AssertHasNonEmptyCorrelationHeaders(response);
    }

    [Fact]
    public async Task CorrelationMiddleware_DoesNothingIfNoHeaders()
    {
        CorrelationIdOptions options = new() { Behavior = CorrelationIdBehavior.IgnoreIfMissing };
        CreateTestServer(options);

        var response =
            await _testServer.CreateRequest("/").GetAsync();

        response.Headers.Should().NotContainKey(CorrelationHeaderNames.CorrelationId)
                .And.NotContainKey(CorrelationHeaderNames.AttemptId);
    }

    [Fact]
    public async Task CorrelationMiddleware_AutoGeneratesCorrelationIdAndAttemptId()
    {
        CorrelationIdOptions options = new() { Behavior = CorrelationIdBehavior.GenerateValues };
        CreateTestServer(options);

        var response =
            await _testServer.CreateRequest("/").GetAsync();

        AssertHasNonEmptyCorrelationHeaders(response);
    }

    [Fact]
    public async Task CorrelationMiddleware_ThrowsInvalidOperationWhenBehaviorSet()
    {
        CorrelationIdOptions options = new() { Behavior = CorrelationIdBehavior.ThrowIfMissing };
        CreateTestServer(options);

        await _testServer.Invoking(x => x.CreateRequest("/").GetAsync())
                        .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CorrelationMiddleware_AddsCorrelationIdAndAttemptIdToResponse()
    {
        CreateTestServer();
        var correlationId = Guid.NewGuid().ToString();
        var attemptId = Guid.NewGuid().ToString();
        var response =
            await _testServer.CreateRequest("/")
                            .AddHeader(name: CorrelationHeaderNames.CorrelationId, value: correlationId)
                            .AddHeader(name: CorrelationHeaderNames.AttemptId, value: attemptId)
                            .GetAsync();

        using (new AssertionScope())
        {
            response.Headers.Should().ContainKey(CorrelationHeaderNames.CorrelationId)
                    .WhoseValue.Single().Should().Be(correlationId);
            response.Headers.Should().ContainKey(CorrelationHeaderNames.AttemptId)
                    .WhoseValue.Single().Should().Be(attemptId);
        }
    }

    [Fact]
    public async Task CorrelationMiddleware_AddsConfiguredPropertiesToProviderNeutralScopeAcrossAwait()
    {
        var loggerProvider = new ScopeCapturingLoggerProvider();
        var options = new CorrelationIdOptions {
            CorrelationIdHeaderName = "X-Request-Id",
            AttemptIdHeaderName = "X-Request-Attempt",
            CorrelationIdPropertyName = "RequestId",
            AttemptIdPropertyName = "RequestAttempt"
        };
        CreateTestServer(options, loggerProvider, async context => {
            await Task.Yield();
            CorrelationContext.CorrelationId.Should().Be("request-correlation");
            CorrelationContext.AttemptId.Should().Be("request-attempt");
            context.RequestServices.GetRequiredService<ILogger<MiddlewareTests>>()
                .LogInformation("Request handled");
        });

        await _testServer.CreateRequest("/")
            .AddHeader("X-Request-Id", "request-correlation")
            .AddHeader("X-Request-Attempt", "request-attempt")
            .GetAsync();

        var properties = loggerProvider.Scopes.SelectMany(scopes => scopes)
            .OfType<IEnumerable<KeyValuePair<string, object?>>>()
            .Single(scope => scope.Any(property => property.Key == "RequestId" &&
                Equals(property.Value, "request-correlation")));
        properties.Any(property => property.Key == "RequestId" && Equals(property.Value, "request-correlation"))
            .Should().BeTrue();
        properties.Any(property => property.Key == "RequestAttempt" && Equals(property.Value, "request-attempt"))
            .Should().BeTrue();
    }

    private static void AssertHasNonEmptyCorrelationHeaders(HttpResponseMessage response)
    {
        using (new AssertionScope())
        {
            response.Headers.Should().ContainKey(CorrelationHeaderNames.CorrelationId)
                    .WhoseValue.Single().Should().HaveLength(36)
                    .And.NotBeEquivalentTo(Guid.Empty.ToString());
            response.Headers.Should().ContainKey(CorrelationHeaderNames.AttemptId)
                    .WhoseValue.Single().Should().HaveLength(36)
                    .And.NotBeEquivalentTo(Guid.Empty.ToString());
        }
    }

    public void Dispose() => _host?.Dispose();

    private sealed class ScopeCapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

        public List<List<object?>> Scopes { get; } = [];

        public ILogger CreateLogger(string categoryName) => new ScopeCapturingLogger(this);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        public void Dispose()
        {
        }

        private sealed class ScopeCapturingLogger(ScopeCapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
                provider._scopeProvider.Push(state);

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                                    Func<TState, Exception?, string> formatter)
            {
                if (!string.Equals(formatter(state, exception), "Request handled", StringComparison.Ordinal))
                    return;

                var scopes = new List<object?>();
                provider._scopeProvider.ForEachScope(static (scope, collection) => collection.Add(scope), scopes);
                provider.Scopes.Add(scopes);
            }
        }
    }
}
