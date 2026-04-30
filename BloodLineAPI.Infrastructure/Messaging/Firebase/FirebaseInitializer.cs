using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BloodLineAPI.Infrastructure.Messaging.Firebase;

public sealed class FirebaseInitializer : IHostedService
{
    private readonly FirebaseOptions _options;
    private readonly ILogger<FirebaseInitializer> _logger;

    public FirebaseInitializer(IOptions<FirebaseOptions> options, ILogger<FirebaseInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), _options.ServiceAccountKeyPath);

                if (!File.Exists(credentialsPath))
                {
                    _logger.LogWarning("Firebase Service Account Key not found at {Path}. Firebase pushed notifications will not work.", credentialsPath);
                    return Task.CompletedTask;
                }

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialsPath),
                    ProjectId = _options.ProjectId
                });

                _logger.LogInformation("FirebaseApp initialized successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase SDK.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}