using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationIdOptions _options;

    public CorrelationIdMiddleware(RequestDelegate next, CorrelationIdOptions options)
    {
        _options = options;
        _next = next;
    }

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
