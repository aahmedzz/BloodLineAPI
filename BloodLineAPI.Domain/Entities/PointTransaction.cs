namespace BloodLineAPI.Domain.Entities
{
    public class PointTransaction : BaseEntity
    {
        public Guid DonorId { get; set; }
        public PointActionType ActionType { get; set; }
        public int Points { get; set; }
        public string Description { get; set; } = string.Empty;
        public string MonthKey { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }

        public Donor Donor { get; set; } = null!;
    }
}
