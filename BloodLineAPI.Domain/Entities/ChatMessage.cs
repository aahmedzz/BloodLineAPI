using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public Guid ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
}
