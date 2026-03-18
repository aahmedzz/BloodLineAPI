namespace BloodLineAPI.Domain.Entities.DonationEntities
{
    public class DonationAppointment : AuditableEntity
    {
        public Guid DonorId { get; set; }
        public Guid DonationCenterId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string DonationType { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public Donor Donor { get; set; } = null!;
        public DonationCenter DonationCenter { get; set; } = null!;
        public BloodBag? BloodBag { get; set; }
        public DonationRating? DonationRating { get; set; }
    }
}
