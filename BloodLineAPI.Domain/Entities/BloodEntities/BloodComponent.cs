namespace BloodLineAPI.Domain.Entities.BloodEntities
{
    public class BloodComponent : AuditableEntity
    {
        public Guid BloodBagId { get; set; }
        public string ComponentType { get; set; } = string.Empty;
        public decimal Volume { get; set; }
        public DateTime ProductionDate { get; set; }
        public DateTime ExpirationDate { get; set; }

        public BloodBag BloodBag { get; set; } = null!;
    }
}
