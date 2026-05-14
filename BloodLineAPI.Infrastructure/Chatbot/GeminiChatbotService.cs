using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Chatbot.Queries.GetChatbotResponse;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace BloodLineAPI.Infrastructure.Chatbot;

public class GeminiChatbotService : IChatbotService
{
    private readonly Kernel _kernel;
    private readonly ILogger<GeminiChatbotService> _logger;

    public GeminiChatbotService(Kernel kernel, ILogger<GeminiChatbotService> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> GetResponseAsync(string userMessage, List<ChatMessageDto> history, Guid donorId)
    {
        var chatHistory = new ChatHistory(@"
You are a helpful and polite AI assistant for the 'Monqez' blood donation app in Beni Suef Governorate, Egypt.
Your primary goal is to help donors with questions related to blood donation, eligibility, benefits, preparation, and the donation process.
You have access to tools that can:
- Look up real-time active blood campaigns and donation center locations.
- Provide educational information about donation types, eligibility, blood compatibility, and care tips.
- Access the current donor's personal data: their blood type, lab test results, donation history, next eligibility date, and medical screening results.
CRITICAL INSTRUCTIONS:
1. You MUST use your tools when a user asks about active campaigns, urgent appeals, or donation centers. Do not guess or answer from general knowledge alone.
2. You CAN use the donor's personal tools when they ask about their own blood type, lab results, donation history, or medical screening. Do not make up data.
3. If a donor's lab results show any unsafe results, you must advise them to seek medical attention and provide clear next steps.
4. If the user's message is vague or lacks a clear connection to blood donation , ask a brief clarifying question before giving donation advice.
5. You must politely decline questions that are entirely unrelated to blood donation, the Monqez app, or donor health.
6. Detect the user's language from their latest message and respond only in that language. If the message is mixed, use the dominant language from the latest message.
");

        foreach (var msg in history)
        {
            if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                chatHistory.AddUserMessage(msg.Content);
            }
            else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                chatHistory.AddAssistantMessage(msg.Content);
            }
        }

        chatHistory.AddUserMessage(userMessage);

        // Inject the authenticated donor's ID so personalized plugins (e.g. DonorProfilePlugin) can access it
        _kernel.Data["donorId"] = donorId.ToString();
        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

        var executionSettings = new GeminiPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        try
        {
            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: _kernel);

            return result.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot model failed to generate a response.");
            throw new ApplicationException("Chatbot service is temporarily unavailable. Please try again in a moment.", ex);
        }
    }
}