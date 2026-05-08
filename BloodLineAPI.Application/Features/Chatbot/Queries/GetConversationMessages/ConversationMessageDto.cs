namespace BloodLineAPI.Application.Features.Chatbot.Queries.GetConversationMessages;

public class ConversationMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
