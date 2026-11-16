using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware;

/// <summary>
/// ASP.NET Core middleware that populates <see cref="CorrelationContext"/> from the incoming request headers and echoes
/// the values back as response headers.
/// </summary>
/// <remarks>
/// <para>
/// For each request the middleware reads the first value of the headers named by
/// <see cref="CorrelationIdOptions.CorrelationIdHeaderName"/> and <see cref="CorrelationIdOptions.AttemptIdHeaderName"/>,
/// then applies <see cref="CorrelationIdOptions.Behavior"/>: generate GUIDs for missing values, leave them
/// <see langword="null"/>, or throw <see cref="MissingCorrelationHeadersException"/>.
/// </para>
/// <para>
/// The resulting values are written to the response headers (when non-null, not already present and the response has
/// not started). Add it with <c>app.UseCorrelationId()</c>.
/// </para>
/// </remarks>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationIdOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="CorrelationIdMiddleware"/>.
    /// </summary>
    /// <param name="next">The next delegate in the request pipeline.</param>
    /// <param name="options">The correlation-id options.</param>
    public CorrelationIdMiddleware(RequestDelegate next, CorrelationIdOptions options)
    {
        _options = options;
        _next = next;
    }

    /// <summary>
    /// Processes the request: sets the correlation context, invokes the next middleware and writes the response
    /// headers.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that completes when the rest of the pipeline has run.</returns>
    /// <exception cref="MissingCorrelationHeadersException">
    /// <see cref="CorrelationIdOptions.Behavior"/> is <see cref="CorrelationIdBehavior.ThrowIfMissing"/> and a header
    /// is missing.
    /// </exception>
    /// <exception cref="EmptyArgumentException">A correlation header is present but has no values.</exception>
    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() => {
            AddCorrelationResponseHeaders(context.Response);
            return Task.CompletedTask;
        });
        SetCorrelationContextFromHeaders(context.Request);
        await _next(context);
        AddCorrelationResponseHeaders(context.Response);
    }
    private Func<string> _generateId = () => Guid.NewGuid().ToString();
    private void SetCorrelationContextFromHeaders(HttpRequest request)
    {
        var correlationId = GetCorrelationId(request);
        var attemptId = GetAttemptId(request);

        if (_options.Behavior == CorrelationIdBehavior.ThrowIfMissing)
        {
            if (correlationId == null || attemptId == null)
                throw new MissingCorrelationHeadersException(_options.CorrelationIdHeaderName, _options.AttemptIdHeaderName);
        }

        if (_options.Behavior == CorrelationIdBehavior.GenerateValues)
        {
            if (correlationId == null) correlationId = _generateId();
            if (attemptId == null) attemptId = _generateId();
        }

        CorrelationContext.SetCorrelationValues(correlationId: correlationId,
            attemptId: attemptId);
    }

    private string? GetCorrelationId(HttpRequest request)
    {
        return GetRequestHeader(request: request, headerName: _options.CorrelationIdHeaderName);
    }

    private string? GetAttemptId(HttpRequest request)
    {
        return GetRequestHeader(request: request, headerName: _options.AttemptIdHeaderName);
    }

    private static string? GetRequestHeader(HttpRequest request, string headerName)
    {
        return !request.Headers.TryGetValue(key: headerName, value: out var values)
            ? null
            : ExtractFirstOfHeaderName(headerName, values);
    }

    private static string? ExtractFirstOfHeaderName(string headerName, Microsoft.Extensions.Primitives.StringValues values)
    {
        if (values.Count == 0)
            throw new EmptyArgumentException(headerName, $"Values empty for {headerName} headers.");

        return values[0];
    }

    private void AddCorrelationResponseHeaders(HttpResponse response)
    {
        if (ShouldWriteToHeader(_options.CorrelationIdHeaderName, CorrelationContext.CorrelationId, response))
        {
            response.Headers.Append(_options.CorrelationIdHeaderName, CorrelationContext.CorrelationId);
        }

        if (ShouldWriteToHeader(_options.AttemptIdHeaderName, CorrelationContext.AttemptId, response))
        {
            response.Headers.Append(_options.AttemptIdHeaderName, CorrelationContext.AttemptId);
        }
    }

    private static bool ShouldWriteToHeader(string header, string? value, HttpResponse response)
    {
        return value != null
               && !response.HasStarted
               && !response.Headers.Any(x => x.Key == header);
    }
}
