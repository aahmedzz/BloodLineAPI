using Microsoft.AspNetCore.Identity;

namespace BloodLineAPI.Domain.Entities.Users
{
    public class User : IdentityUser<Guid>
    {
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? RegistrationOtpCode { get; set; }
        public DateTime? RegistrationOtpExpiryTime { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
        public ICollection<ChatConversation> ChatConversations { get; set; } = new List<ChatConversation>();
        public Donor? Donor { get; set; }
        public Staff? Staff { get; set; }
    }
}
