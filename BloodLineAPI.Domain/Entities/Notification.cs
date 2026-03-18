namespace BloodLineAPI.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime SentDate { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
