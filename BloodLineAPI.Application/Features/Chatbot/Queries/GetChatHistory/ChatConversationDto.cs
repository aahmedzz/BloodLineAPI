namespace BloodLineAPI.Application.Features.Chatbot.Queries.GetChatHistory;

public class ChatConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
}
