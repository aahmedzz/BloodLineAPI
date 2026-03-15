namespace BloodBankSystem.Domain.Entities.DonationEntities
{
    public class DonationRating : AuditableEntity
    {
        public Guid DonorId { get; set; }
        public Guid DonationAppointmentId { get; set; }
        public int StarScore { get; set; }
        public string? FeedbackText { get; set; }
        public DateTime SubmittedAt { get; set; }

        public Donor Donor { get; set; } = null!;
        public DonationAppointment DonationAppointment { get; set; } = null!;
    }
}
