using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BloodLineAPI.Infrastructure.Messaging;

public sealed class WaSenderApiWhatsappMessageSender(
    HttpClient httpClient,
    IOptions<WaSenderApiOptions> options,
    ILogger<WaSenderApiWhatsappMessageSender> logger) : IWhatsappMessageSender
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly WaSenderApiOptions _options = options.Value;
    private readonly ILogger<WaSenderApiWhatsappMessageSender> _logger = logger;

    public async Task<bool> SendVerificationOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("WaSenderAPI key is missing.");
                return false;
            }

            var to = NormalizePhoneNumber(phoneNumber);
            var payload = new
            {
                to,
                text = $"رمز التحقق الخاص بك في BloodLink هو: {otpCode}\nصالح لمدة 10 دقائق.\nمن فضلك لا تشاركه مع أي شخص ❤️"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.SendEndpoint)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var failureBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to send OTP using WaSenderAPI. StatusCode: {StatusCode}, Body: {Body}", response.StatusCode, failureBody);
                return false;
            }

            var responseBody = await response.Content.ReadFromJsonAsync<WaSenderResponse>(cancellationToken);
            return responseBody?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending OTP using WaSenderAPI.");
            return false;
        }
    }

    private string NormalizePhoneNumber(string phoneNumber)
    {
        var normalized = phoneNumber.Trim();
        if (normalized.StartsWith('+'))
        {
            return normalized;
        }

        if (normalized.StartsWith('0'))
        {
            return $"{_options.DefaultCountryCode}{normalized}";
        }

        return normalized;
    }

    private sealed class WaSenderResponse
    {
        public bool Success { get; set; }
    }
}
