using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ATSolution.Api.OpenApi;

/// <summary>
/// Registers JWT Bearer in the OpenAPI document and requires it on every operation
/// so Scalar/clients can attach <c>Authorization: Bearer &lt;token&gt;</c> automatically.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    private const string SchemeId = "Bearer";

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(scheme => string.Equals(scheme.Name, SchemeId, StringComparison.Ordinal)))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the JWT from POST /api/auth/login. Scalar will send it as Authorization: Bearer {token}.",
        };

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeId, document)] = [],
        };

        // Document-level default — Scalar prefers this scheme for every request.
        document.Security ??= [];
        if (!document.Security.Any(HasBearerRequirement))
        {
            document.Security.Add(requirement);
        }

        if (document.Paths is null)
        {
            return;
        }

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem.Operations is null)
            {
                continue;
            }

            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Security ??= [];
                if (!operation.Security.Any(HasBearerRequirement))
                {
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(SchemeId, document)] = [],
                    });
                }
            }
        }
    }

    private static bool HasBearerRequirement(OpenApiSecurityRequirement requirement) =>
        requirement.Keys.Any(key =>
            key is OpenApiSecuritySchemeReference reference
            && string.Equals(reference.Reference?.Id, SchemeId, StringComparison.Ordinal));
}
