using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Entities.Users;

namespace BloodLineAPI.Domain.Entities;

public class ChatConversation : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
