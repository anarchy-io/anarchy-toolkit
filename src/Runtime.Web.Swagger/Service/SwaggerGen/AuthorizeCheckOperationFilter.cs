using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Anarchy.Toolkit.Runtime.Web.Swagger.Service.SwaggerGen;

/// <summary>
/// Swagger operation filter that documents authorization requirements for actions protected by
/// <see cref="AuthorizeAttribute"/>.
/// </summary>
/// <remarks>
/// Actions marked with <see cref="AllowAnonymousAttribute"/>, or without any <see cref="AuthorizeAttribute"/> on the
/// action or its controller, are left untouched. Otherwise <c>401</c> and <c>403</c> responses are added and the
/// operation requires the <c>oauth2</c> security scheme, listing the distinct authorization policy names as scopes.
/// Registered by <c>AddToolkitSwaggerGen</c>.
/// </remarks>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    /// <summary>Adds the authorization responses and security requirement to <paramref name="operation"/> when applicable.</summary>
    /// <param name="operation">The OpenAPI operation being generated.</param>
    /// <param name="context">The operation filter context, providing the action method.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var allowAnon = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (allowAnon) return;

        var authorizeAttrs = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Concat(context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>() ?? Array.Empty<AuthorizeAttribute>())
            .ToArray();

        if (authorizeAttrs.Length == 0) return;

        operation.Responses?.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses?.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

        var requiredScopes = authorizeAttrs
            .Where(a => !string.IsNullOrEmpty(a.Policy))
            .Select(a => a.Policy!)
            .Distinct()
            .ToList();

        operation.Security = new List<OpenApiSecurityRequirement> {
            new() {
                [new OpenApiSecuritySchemeReference("oauth2", context.Document)] = requiredScopes
            }
        };
    }
}
