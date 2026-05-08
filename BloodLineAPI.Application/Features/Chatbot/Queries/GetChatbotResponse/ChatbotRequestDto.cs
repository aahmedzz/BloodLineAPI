using System.ComponentModel.DataAnnotations;

namespace BloodLineAPI.Application.Features.Chatbot.Queries.GetChatbotResponse;

public class ChatbotRequestDto
{
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional. Pass an existing conversation ID to continue a conversation.
    /// If null, a new conversation is created.
    /// </summary>
    public Guid? ConversationId { get; set; }
}