using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Libr4.Shared.Web.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddLibr4Swagger(
        this IServiceCollection services,
        string title,
        string version = "v1",
        bool includeSecurity = true,
        bool includeBearer = true)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });
            if (includeSecurity || includeBearer)
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Bearer token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
                });
            }
        });
        return services;
    }
}
