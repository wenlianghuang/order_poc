using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OrderService.Api.OpenApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOrderServiceOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
        });

        return services;
    }

    public static WebApplication MapOrderServiceOpenApi(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "OrderService";
            options.SwaggerEndpoint("/openapi/v1.json", "OrderService v1");
            options.EnablePersistAuthorization();
        });

        return app;
    }

    private sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
        : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (authenticationSchemes.All(scheme => scheme.Name != "Bearer"))
                return;

            document.Info = new OpenApiInfo
            {
                Title = "OrderService",
                Version = "v1"
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
                    Description = "Paste the token returned by POST /login. Leave off the Bearer prefix."
                }
            };
        }
    }

    private sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<IAuthorizeData>()
                .Any();

            if (!requiresAuthorization)
                return Task.CompletedTask;

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });

            return Task.CompletedTask;
        }
    }
}
