using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Repositories;
using BloodLineAPI.Infrastructure.BackgroundJobs;
using BloodLineAPI.Infrastructure.Authentication;
using BloodLineAPI.Infrastructure.Messaging;
using BloodLineAPI.Infrastructure.Persistence;
using BloodLineAPI.Infrastructure.Repositories;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BloodLineAPI.Infrastructure.Messaging.Firebase;

using Microsoft.SemanticKernel;

namespace BloodLineAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IRegistrationOtpService, RegistrationOtpService>();

        var firebaseSection = configuration.GetSection(FirebaseOptions.SectionName);
        if (firebaseSection.Exists() && !string.IsNullOrWhiteSpace(firebaseSection["ServiceAccountKeyPath"]))
        {
            services.Configure<FirebaseOptions>(firebaseSection);
            services.AddHostedService<FirebaseInitializer>();
            services.AddScoped<INotificationSender, FirebaseNotificationSender>();
        }
        else
        {
            services.AddScoped<INotificationSender, NoOpNotificationSender>();
        }

        services.AddScoped<AppointmentReminderJob>();
        services.AddScoped<ChatHistoryCleanupJob>();

        services.Configure<DonationCooldownSettings>(configuration.GetSection("DonationCooldown"));
        services.Configure<AppointmentSettings>(configuration.GetSection("Appointment"));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddHangfire(config => config.UseSqlServerStorage(connectionString, new SqlServerStorageOptions()));
            services.AddHangfireServer();
        }

        services.Configure<WaSenderApiOptions>(configuration.GetSection("WaSenderApi"));
        services.AddHttpClient<IWhatsappMessageSender, WaSenderApiWhatsappMessageSender>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WaSenderApiOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }
        });

        // Chatbot (Gemini) configuration
        var geminiApiKey = configuration["Gemini:ApiKey"];
        var geminiModel = configuration["Gemini:Model"] ?? "gemini-flash-latest";

        if (!string.IsNullOrWhiteSpace(geminiApiKey))
        {
            services.AddScoped<BloodLineAPI.Infrastructure.Chatbot.Plugins.BloodLineDataPlugin>();
            services.AddScoped<BloodLineAPI.Infrastructure.Chatbot.Plugins.DonorProfilePlugin>();
            services.AddSingleton<BloodLineAPI.Infrastructure.Chatbot.Plugins.DonationKnowledgePlugin>();

            services.AddScoped<Kernel>(sp =>
            {
                var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
                builder.AddGoogleAIGeminiChatCompletion(modelId: geminiModel, apiKey: geminiApiKey);
#pragma warning restore SKEXP0070
                builder.Plugins.AddFromObject(sp.GetRequiredService<BloodLineAPI.Infrastructure.Chatbot.Plugins.BloodLineDataPlugin>());
                builder.Plugins.AddFromObject(sp.GetRequiredService<BloodLineAPI.Infrastructure.Chatbot.Plugins.DonationKnowledgePlugin>());
                builder.Plugins.AddFromObject(sp.GetRequiredService<BloodLineAPI.Infrastructure.Chatbot.Plugins.DonorProfilePlugin>());
                return builder.Build();
            });
            services.AddScoped<BloodLineAPI.Application.Common.Interfaces.IChatbotService, BloodLineAPI.Infrastructure.Chatbot.GeminiChatbotService>();
        }
        else
        {
            // Register a dummy service if the API key is not configured, otherwise MediatR validation fails
            services.AddScoped<BloodLineAPI.Application.Common.Interfaces.IChatbotService, BloodLineAPI.Infrastructure.Chatbot.NoOpChatbotService>();
        }

        return services;
    }
}
