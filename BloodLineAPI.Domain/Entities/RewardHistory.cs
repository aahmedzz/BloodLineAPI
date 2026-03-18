namespace BloodLineAPI.Domain.Entities
{
    public class RewardHistory : BaseEntity
    {
        public Guid DonorId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public int PointsAwarded { get; set; }
        public DateTime TransactionDate { get; set; }

        public Donor Donor { get; set; } = null!;
    }

}
