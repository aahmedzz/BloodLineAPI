using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Entities.Users;
using BloodLineAPI.Filters;
using BloodLineAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

namespace BloodLineAPI;

public static class DependencyInjection
{
    private static readonly string[] ApiVersions = ["v1"];

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ApiResponseWrapperFilter>();
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("Jwt Secret is not configured.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = false,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"));
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        const string audienceDescription =
            "Endpoints are tagged by target audience:\n" +
            "- **System** – Used by the web administration system\n" +
            "- **Mobile** – Used by the mobile application";

        foreach (var version in ApiVersions)
        {
            services.AddOpenApi(version, options =>
            {
                options.AddDocumentTransformer((doc, ctx, ct) =>
                {
                    doc.Info.Title = "BloodLine API";
                    doc.Info.Version = version;
                    doc.Info.Description = $"BloodLine platform API {version} endpoints.\n\n{audienceDescription}";
                    return Task.CompletedTask;
                });
                AddJwtSecurity(options);
                AddAudienceTagging(options);
                AddApiResponseSchemaFix(options);
            });
        }

        return services;
    }

    private static void AddAudienceTagging(OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, ct) =>
        {
            var audienceAttr = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<ApiAudienceAttribute>()
                .FirstOrDefault();

            if (audienceAttr is not null)
            {
                var controller = context.Description.ActionDescriptor.RouteValues["controller"];
                operation.Tags = new HashSet<OpenApiTagReference>
                {
                    new(audienceAttr.Audience + " - " + controller)
                };
            }


            return Task.CompletedTask;
        });
    }

    private static void AddJwtSecurity(OpenApiOptions options)
    {
        options.AddDocumentTransformer((doc, ctx, ct) =>
        {
            doc.Components ??= new OpenApiComponents();
            doc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            var scheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Enter: {JWT token} into the field below (without the 'Bearer' prefix)."
            };

            doc.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = scheme;

            doc.Security ??= new List<OpenApiSecurityRequirement>();
            doc.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, doc)] = new List<string>()
            });

            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, ct) =>
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            var isAnonymous = metadata.OfType<IAllowAnonymous>().Any();
            var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();

            if (isAnonymous || !requiresAuthorization)
            {
                operation.Security = new List<OpenApiSecurityRequirement>();
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Fixes the OpenAPI schema so that Swagger UI shows the full typed data structure
    /// in Example Value instead of null. The .NET OpenAPI generator creates oneOf [null, $ref]
    /// for nullable generic type parameters, which Swagger UI renders as the first option (null).
    /// This transformer replaces that pattern with just the $ref so the example is useful.
    /// </summary>
    private static void AddApiResponseSchemaFix(OpenApiOptions options)
    {
        options.AddDocumentTransformer((doc, ctx, ct) =>
        {
            if (doc.Components?.Schemas is null)
                return Task.CompletedTask;

            foreach (var (schemaName, schemaValue) in doc.Components.Schemas)
            {
                if (schemaValue is not OpenApiSchema schema || schema.Properties is null)
                    continue;

                // Fix nullable $ref properties rendered as oneOf [null, $ref].
                // Swagger UI often picks null as Example Value and hides object shape.
                foreach (var (propertyName, propertyValue) in schema.Properties)
                {
                    if (propertyValue is OpenApiSchema propertySchema)
                    {
                        FixNullableOneOf(schema, propertyName, propertySchema);
                    }
                }

                // Keep the specific ApiResponse<T> "errors" object fix.
                if (schemaName.StartsWith("ApiResponseOf", StringComparison.Ordinal)
                    && schema.Properties.TryGetValue("errors", out var errorsProp))
                {
                    if (errorsProp is OpenApiSchema errSchema && 
                        (errSchema.Type is null || errSchema.Type == JsonSchemaType.Null))
                    {
                        schema.Properties["errors"] = new OpenApiSchema
                        {
                            Description = "Validation or error details. Only present when the request fails.",
                            Type = JsonSchemaType.Object | JsonSchemaType.Null,
                            AdditionalPropertiesAllowed = true
                        };
                    }
                }
            }

            return Task.CompletedTask;
        });
    }

    private static void FixNullableOneOf(OpenApiSchema parentSchema, string propertyName, OpenApiSchema dataPropSchema)
    {
        // The pattern we're fixing: oneOf: [{ type: "null" }, { $ref: "..." }]
        if (dataPropSchema.OneOf is not { Count: 2 })
            return;

        // Find the non-null option (the $ref)
        OpenApiSchemaReference? refSchema = null;
        foreach (var option in dataPropSchema.OneOf)
        {
            if (option is OpenApiSchemaReference schemaRef)
            {
                refSchema = schemaRef;
                break;
            }
        }

        if (refSchema is null)
            return;

        // Replace the oneOf with just the $ref so Swagger shows the full data shape
        parentSchema.Properties![propertyName] = refSchema;
    }
}
