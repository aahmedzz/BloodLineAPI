namespace BloodLineAPI.Domain.Entities
{
    public class Donor : AuditableEntity
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public BloodType? BloodType { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? LastDonationDate { get; set; }
        public int TotalPoints { get; set; } = 0;
        public bool AllowLeaderboardVisibility { get; set; } = true;

        public User User { get; set; } = null!;
        public ICollection<DonationAppointment> DonationAppointments { get; set; } = new List<DonationAppointment>();
        public ICollection<RewardHistory> RewardHistories { get; set; } = new List<RewardHistory>();
        public ICollection<DonorBadge> DonorBadges { get; set; } = new List<DonorBadge>();
        public ICollection<DonationRating> DonationRatings { get; set; } = new List<DonationRating>();

    }
}
