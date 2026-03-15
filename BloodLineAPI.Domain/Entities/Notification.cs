namespace BloodBankSystem.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime SentDate { get; set; }

        public User User { get; set; } = null!;
    }
}
