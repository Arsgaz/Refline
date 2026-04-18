using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Refline.Api.Swagger;

public sealed class InternalApiKeyOperationFilter : IOperationFilter
{
    private const string InternalPrefix = "api/internal/";
    private const string SecuritySchemeId = "ReflineInternalApiKey";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath;
        if (string.IsNullOrWhiteSpace(relativePath) ||
            !relativePath.StartsWith(InternalPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = SecuritySchemeId
                    }
                }
            ] = Array.Empty<string>()
        });
    }
}
