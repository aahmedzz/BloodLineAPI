
namespace BloodBankSystem.Domain.Entities
{
    public class Badge : BaseEntity
    {
        public string BadgeName { get; set; } = string.Empty;
        public string BadgeDescription { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int RequiredPoints { get; set; }
        public ICollection<DonorBadge> DonorBadges { get; set; } = new List<DonorBadge>();
    }
}
