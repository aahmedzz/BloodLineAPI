using System.ComponentModel.DataAnnotations.Schema;

namespace BloodLineAPI.Domain.Entities.BloodEntities
{
    public class BloodType
    {
        public byte Id { get; set; }
        public BloodGroupName BloodGroupName { get; set; }
        public RhFactor RhFactor { get; set; }

        [NotMapped]
        public string FullDisplayname => $"{BloodGroupName.ToString()}{(RhFactor == RhFactor.Positive ? "+" : "-")}";

        public ICollection<Donor> Donors { get; set; } = new List<Donor>();
        public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
        public ICollection<UrgentBloodAppeal> UrgentBloodAppeals { get; set; } = new List<UrgentBloodAppeal>();
    }
}
