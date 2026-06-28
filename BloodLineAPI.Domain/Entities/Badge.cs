
namespace BloodLineAPI.Domain.Entities
{
    public class Badge : BaseEntity
    {
        public string BadgeKey { get; set; } = string.Empty;
        public string BadgeName { get; set; } = string.Empty;
        public string BadgeNameAr { get; set; } = string.Empty;
        public string BadgeDescription { get; set; } = string.Empty;
        public string BadgeDescriptionAr { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public BadgeType BadgeType { get; set; }
        public int BonusPoints { get; set; }
        public ICollection<DonorBadge> DonorBadges { get; set; } = new List<DonorBadge>();
    }
}
