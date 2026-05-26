namespace BloodLineAPI.Domain.Entities
{
    public class Donor : AuditableEntity
    {
        public int DonorNumber { get; private set; }
        public string DonorCode { get; private set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string ThirdName { get; set; } = string.Empty;
        public string? FourthName { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? Governorate { get; set; }
        public string? District { get; set; }
        public string? Area { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Gender Gender { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public byte? BloodTypeId { get; set; }
        public BloodType? BloodType { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal? WeightKg { get; set; }
        public bool IsRegistrationCompleted { get; set; } = false;
        public DateTime? LastDonationDate { get; set; }
        public int TotalPoints { get; set; } = 0;
        public int MonthlyPoints { get; set; } = 0;
        public int TotalDonationCount { get; set; } = 0;
        public bool AllowLeaderboardVisibility { get; set; } = true;
        public DonorStatus Status { get; set; } = DonorStatus.Eligible;
        public string FullName => string.Join(" ", new[] { FirstName, SecondName, ThirdName, FourthName }
            .Where(static n => !string.IsNullOrWhiteSpace(n)));

        public User User { get; set; } = null!;
        public ICollection<DonationAppointment> DonationAppointments { get; set; } = new List<DonationAppointment>();
        public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
        public ICollection<DonorBadge> DonorBadges { get; set; } = new List<DonorBadge>();
        public ICollection<DonationRating> DonationRatings { get; set; } = new List<DonationRating>();
        public ICollection<HealthPreScreening> HealthPreScreenings { get; set; } = new List<HealthPreScreening>();
        public ICollection<MedicalScreening> MedicalScreenings { get; set; } = new List<MedicalScreening>();

    }
}
