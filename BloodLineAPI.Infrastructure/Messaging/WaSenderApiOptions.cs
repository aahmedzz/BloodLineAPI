namespace BloodLineAPI.Infrastructure.Messaging;

public sealed class WaSenderApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SendEndpoint { get; set; } = "/api/send-message";
    public string? ApiKey { get; set; }
    public string DefaultCountryCode { get; set; } = "+2";
}
