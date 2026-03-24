using Asp.Versioning;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Entities.Users;
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
        services.AddControllers();

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
                AddAudienceTagging(options);
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
}
