using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Anarchy.Toolkit.Runtime.Web.Swagger.Service.SwaggerGen;

/// <summary>
///     Documents the optional <c>Accept-Encoding</c> header on every operation, so generated
///     API clients default to requesting compressed (gzip/br) responses.
/// </summary>
public class AcceptEncodingHeaderOperationFilter : IOperationFilter
{
    /// <summary>Adds the optional <c>Accept-Encoding</c> header parameter (default <c>gzip, br</c>) to <paramref name="operation"/>.</summary>
    /// <param name="operation">The OpenAPI operation being generated.</param>
    /// <param name="context">The operation filter context.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter {
            Name = "Accept-Encoding",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema {
                Type = JsonSchemaType.String,
                Default = "gzip, br"
            },
            Description = "Preferred response content encoding. Defaults to \"gzip, br\" to enable response compression."
        });
    }
}
