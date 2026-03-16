namespace BloodLineAPI.Domain.Entities.DonationEntities
{
    public class DonationCenter : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AddressDetails { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string CenterType { get; set; } = string.Empty;
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? DescriptionText { get; set; }
        public int MaxDonorsPerSlot { get; set; }
        public int? SlotDurationMinutes { get; set; }

        public ICollection<DonationAppointment> DonationAppointments { get; set; } = new List<DonationAppointment>();
    }
}
