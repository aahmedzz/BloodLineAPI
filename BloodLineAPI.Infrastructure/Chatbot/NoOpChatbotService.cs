using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Chatbot.Queries.GetChatbotResponse;

namespace BloodLineAPI.Infrastructure.Chatbot;

public class NoOpChatbotService : IChatbotService
{
    public Task<string> GetResponseAsync(string userMessage, List<ChatMessageDto> history, Guid donorId)
    {
        return Task.FromResult("Chatbot service is not configured. Please check your AI provider settings.");
    }
}