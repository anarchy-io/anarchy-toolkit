using Anarchy.Toolkit.Samples.Ui.Infrastructure.Entity.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Anarchy.Toolkit.Samples.Ui.Web.Stub;

/// <summary>
/// Stand-ins for an OAuth server and a downstream orders API, so the sample runs without external services.
/// Not part of the toolkit; a real UI talks to real hosts.
/// </summary>
public static class StubEndpointRouteBuilderExtension
{
    private const string TokenPrefix = "stub-";

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapStubEndpoints()
        {
            var stub = endpoints.MapGroup("stub")
                .AllowAnonymous()
                .DisableAntiforgery()
                .ExcludeFromDescription();

            stub.MapPost("auth/connect/token", IssueToken);

            var api = stub.MapGroup("{environment}").AddEndpointFilter(RequireStubToken);
            api.MapGet("orders", GetOrders);
            api.MapPost("orders/{id}/cancel", CancelOrder);

            return endpoints;
        }
    }

    private static IResult IssueToken(
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "client_secret")] string? clientSecret,
        AppSettings settings)
    {
        if (clientId != settings.StubClientId || clientSecret != settings.StubClientSecret)
        {
            return Results.Json(new { error = "invalid_client" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Json(new Dictionary<string, object> {
            ["access_token"] = TokenPrefix + Guid.NewGuid().ToString("N"),
            ["token_type"] = "Bearer",
            ["expires_in"] = 3600,
        });
    }

    private static async ValueTask<object?> RequireStubToken(EndpointFilterInvocationContext context,
                                                             EndpointFilterDelegate next)
    {
        var authorization = context.HttpContext.Request.Headers.Authorization.ToString();

        return authorization.StartsWith($"Bearer {TokenPrefix}", StringComparison.Ordinal)
            ? await next(context)
            : Results.Problem(title: "Unauthorized", detail: "A stub access token is required.",
                              statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult GetOrders(string environment, StubOrderStore store) =>
        store.GetOrders(environment) is { } orders
            ? Results.Ok(orders)
            : Results.Problem(title: "Not Found", detail: $"Unknown environment '{environment}'.",
                              statusCode: StatusCodes.Status404NotFound);

    private static IResult CancelOrder(string environment, string id, StubOrderStore store) =>
        store.Cancel(environment, id) switch {
            StubOrderStore.CancelResult.Cancelled => Results.NoContent(),
            StubOrderStore.CancelResult.NotCancellable => Results.Problem(
                title: "Conflict", detail: $"Order {id} has already shipped or been cancelled.",
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(title: "Not Found", detail: $"Order {id} does not exist.",
                                 statusCode: StatusCodes.Status404NotFound),
        };
}
