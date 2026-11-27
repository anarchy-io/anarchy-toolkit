using Anarchy.Toolkit.Runtime.Web.MockServer.Extensions.Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Samples.MockServer.Middlewares;

/// <summary>
/// Computes <c>GET /stock/{sku}</c> from the request instead of listing every SKU in a file: SKUs ending in
/// <c>-0</c> are out of stock, the rest have as many units as their length. A JSON expectation for a specific SKU
/// still wins, because this only answers when no expectation matched.
/// </summary>
public static class StockMiddleware
{
    public static async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var mock = context.GetMockServer();
        var segments = mock.Request.Path.Trim('/').Split('/');

        if (mock.Expectation is null && mock.Request.Method == HttpMethods.Get && segments is ["stock", var sku])
        {
            mock.AnsweredBy = "stock middleware";
            await context.Response.WriteAsJsonAsync(new { sku, available = sku.EndsWith("-0") ? 0 : sku.Length });
            return;
        }

        await next(context);
    }
}
