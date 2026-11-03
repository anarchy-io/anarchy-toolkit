using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Anarchy.Toolkit.Web.Service.SwaggerGen;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
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
