namespace BloodBankSystem.Domain.Entities.BloodEntities
{
    public class BloodTypeEntity : BaseEntity
    {
        public BloodType BloodGroupName { get; set; }
        public RhFactor RhFactor { get; set; }

        public ICollection<Donor> Donors { get; set; } = new List<Donor>();
        public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
        public ICollection<UrgentBloodAppeal> UrgentBloodAppeals { get; set; } = new List<UrgentBloodAppeal>();
    }
}
