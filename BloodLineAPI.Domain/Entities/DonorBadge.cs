namespace BloodBankSystem.Domain.Entities
{
    public class DonorBadge : BaseEntity
    {
        public Guid DonorId { get; set; }
        public Guid BadgeId { get; set; }
        public DateTime EarnedDate { get; set; }

        public Donor Donor { get; set; } = null!;
        public Badge Badge { get; set; } = null!;
    }
}
