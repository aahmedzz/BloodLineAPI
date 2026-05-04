using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public bool IsSent { get; set; } = false;
        public string? SentVia { get; set; } // "fcm", etc.
        public DateTime SentDate { get; set; }

        /// <summary>
        /// JSON blob with entity-only context for frontend routing.
        /// Example: {"targetEntity":"DonationCenter","targetId":"d4e5f6a7-...","appointmentId":"e5f6a7b8-..."}
        /// The backend never specifies routes — Flutter decides navigation from this context.
        /// </summary>
        public string? ActionPayload { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
