using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Chatbot.Queries.GetChatbotResponse;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IChatbotService
{
    Task<string> GetResponseAsync(string userMessage, List<ChatMessageDto> history, Guid donorId);
}