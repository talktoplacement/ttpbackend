using Microsoft.OpenApi.Models;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// OpenAPI / Swagger registration. Publishes a single <c>v1</c> document at
/// <c>/swagger/v1/swagger.json</c> and a browsable UI at <c>/swagger</c>. Every controller
/// declares its route directly in the canonical form (<c>/api/v1/{kebab-case-resource}</c>),
/// so no path rewriting is applied to the generated document.
///
/// The <c>Bearer</c> security scheme is declared globally so every endpoint's "Authorize" button
/// in the UI accepts a Supabase JWT (mobile clients send <c>Authorization: Bearer &lt;token&gt;</c>).
/// </summary>
public static class OpenApiRegistration
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CareerPlatform API",
                Version = "v1",
                Description =
                    "REST API for the CareerPlatform / TalktoPlacement platform.\n\n"
                    + "**Base URL:** `/api/v1/{resource}` — lowercase, kebab-case, plural nouns. "
                    + "This is the sole public surface; there is no unversioned or PascalCase form.\n\n"
                    + "**Authentication:** Bearer JWT (Supabase-issued). Send "
                    + "`Authorization: Bearer <token>` on every authenticated request.\n\n"
                    + "**HTTP methods:** `GET` (read), `POST` (create / RPC-style actions such as "
                    + "`.../read-all`), `PUT` (full-resource update), `DELETE` (remove). Partial "
                    + "updates use `PUT` with the full resource — there are no `PATCH` endpoints.",
            });

            // Every authenticated endpoint reads Authorization: Bearer <token>; declare the scheme
            // once and apply it globally so the UI shows the padlock everywhere.
            var bearer = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Supabase-issued JWT. Format: `Bearer <token>`.",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            };
            c.AddSecurityDefinition("Bearer", bearer);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [bearer] = Array.Empty<string>(),
            });
        });

        return services;
    }
}
