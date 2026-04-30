namespace BloodLineAPI.Domain.Entities;

using BloodLineAPI.Domain.Entities.Users;

public class DeviceToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // "android" | "ios"
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}