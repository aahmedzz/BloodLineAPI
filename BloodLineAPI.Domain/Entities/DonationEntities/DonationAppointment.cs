namespace BloodLineAPI.Domain.Entities.DonationEntities
{
    public class DonationAppointment : AuditableEntity
    {
        public Guid DonorId { get; private set; }
        public Guid DonationCenterId { get; private set; }
        public Guid? HealthPreScreeningId { get; private set; }
        public DateTime ScheduledDate { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }
        public DonationType DonationType { get; private set; }
        public AppointmentStatus Status { get; private set; } = AppointmentStatus.Pending;
        public DonationSource Source { get; private set; } = DonationSource.WalkIn;
        public string? CancellationReason { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public byte[] RowVersion { get; private set; } = [];

        public Donor Donor { get; set; } = null!;
        public DonationCenter DonationCenter { get; set; } = null!;
        public HealthPreScreening? HealthPreScreening { get; set; }
        public BloodBag? BloodBag { get; set; }
        public DonationRating? DonationRating { get; set; }

        private DonationAppointment()
        {
        }

        /// <summary>
        /// Factory method to create a new appointment.
        /// Operating hours validation is done by the handler; this method
        /// enforces capacity, cooldown, and lockout invariants.
        /// </summary>
        public static DonationAppointment Book(
            Guid donorId,
            Guid donationCenterId,
            DateTime scheduledDate,
            TimeSpan startTime,
            int slotDurationMinutes,
            DonationType donationType,
            Guid? healthPreScreeningId,
            int existingBookingsInSlot,
            int maxDonorsPerSlot,
            DateTime? lastDonationDate,
            DateTime? activeLockoutUntil,
            Gender donorGender,
            DonationCooldownSettings cooldownSettings,
            DonationSource source = DonationSource.WalkIn)
        {
            if (scheduledDate.Date < DateTime.UtcNow.Date)
            {
                throw new DomainException("Cannot book an appointment in the past.");
            }

            if (existingBookingsInSlot >= maxDonorsPerSlot)
            {
                throw new DomainException("This time slot is fully booked.");
            }

            var cooldownDays = cooldownSettings.GetCooldownDays(donationType, donorGender);
            if (lastDonationDate.HasValue && (scheduledDate.Date - lastDonationDate.Value.Date).TotalDays < cooldownDays)
            {
                throw new DomainException($"Must wait {cooldownDays} days between donations.");
            }

            if (activeLockoutUntil.HasValue && DateTime.UtcNow < activeLockoutUntil.Value)
            {
                throw new DomainException(
                    $"You are locked out from booking until {activeLockoutUntil.Value:yyyy-MM-dd} due to a failed medical screening.");
            }

            return new DonationAppointment
            {
                Id = Guid.NewGuid(),
                DonorId = donorId,
                DonationCenterId = donationCenterId,
                HealthPreScreeningId = healthPreScreeningId,
                ScheduledDate = scheduledDate.Date,
                StartTime = startTime,
                EndTime = startTime.Add(TimeSpan.FromMinutes(slotDurationMinutes)),
                DonationType = donationType,
                Status = AppointmentStatus.Pending,
                Source = source
            };
        }

        public void Cancel(string reason, int gracePeriodMinutes = 30)
        {
            if (Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
            {
                throw new DomainException("Cannot cancel an appointment that is already cancelled or completed.");
            }

            var appointmentStart = ScheduledDate.Date.Add(StartTime);
            if (appointmentStart <= DateTime.UtcNow)
            {
                throw new DomainException("Cannot cancel an appointment that has already started or passed.");
            }

            if ((appointmentStart - DateTime.UtcNow).TotalMinutes < gracePeriodMinutes)
            {
                throw new DomainException($"Cannot cancel within {FormatGracePeriod(gracePeriodMinutes)} of the appointment. Please contact the center directly.");
            }

            Status = AppointmentStatus.Cancelled;
            CancellationReason = reason;
            CancelledAt = DateTime.UtcNow;
        }

        public void CancelDueToIneligiblePreScreening()
        {
            if (Status is AppointmentStatus.Completed or AppointmentStatus.NoShow)
            {
                throw new DomainException("Cannot cancel an appointment that is already completed or marked as no-show.");
            }

            if (Status == AppointmentStatus.Cancelled)
            {
                return;
            }

            Status = AppointmentStatus.Cancelled;
            CancellationReason = "Cancelled automatically due to ineligible health pre-screening.";
            CancelledAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Reschedules the appointment to a new date/time/center.
        /// Operating hours validation is done by the handler; this method
        /// enforces grace period, capacity, and state invariants.
        /// </summary>
        public void Reschedule(
            Guid newCenterId,
            DateTime newDate,
            TimeSpan newStartTime,
            int slotDurationMinutes,
            int existingBookingsInSlot,
            int maxDonorsPerSlot,
            int gracePeriodMinutes = 30)
        {
            if (Status is not AppointmentStatus.Pending and not AppointmentStatus.Confirmed)
            {
                throw new DomainException("Only pending or confirmed appointments can be rescheduled.");
            }

            var currentStart = ScheduledDate.Date.Add(StartTime);
            if (currentStart <= DateTime.UtcNow)
            {
                throw new DomainException("Cannot reschedule a past appointment.");
            }

            if ((currentStart - DateTime.UtcNow).TotalMinutes < gracePeriodMinutes)
            {
                throw new DomainException($"Cannot reschedule within {FormatGracePeriod(gracePeriodMinutes)} of the appointment. Please contact the center directly.");
            }

            if (newDate.Date < DateTime.UtcNow.Date)
            {
                throw new DomainException("Cannot reschedule to a past date.");
            }

            if (existingBookingsInSlot >= maxDonorsPerSlot)
            {
                throw new DomainException("This time slot is fully booked.");
            }

            ScheduledDate = newDate.Date;
            StartTime = newStartTime;
            EndTime = newStartTime.Add(TimeSpan.FromMinutes(slotDurationMinutes));
            DonationCenterId = newCenterId;
        }

        public void Confirm()
        {
            if (Status != AppointmentStatus.Pending)
            {
                throw new DomainException("Only pending appointments can be confirmed.");
            }

            Status = AppointmentStatus.Confirmed;
        }

        public void AttachHealthPreScreening(Guid donorId, Guid screeningId)
        {
            if (DonorId != donorId)
            {
                throw new DomainException("This appointment does not belong to the donor.");
            }

            if (Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
            {
                throw new DomainException("Cannot attach pre-screening to this appointment status.");
            }

            if (HealthPreScreeningId.HasValue)
            {
                throw new DomainException("A pre-screening is already attached to this appointment.");
            }

            HealthPreScreeningId = screeningId;
        }

        public void MarkCompleted()
        {
            if (Status is not AppointmentStatus.Pending and not AppointmentStatus.Confirmed)
            {
                throw new DomainException("Only pending or confirmed appointments can be marked as completed.");
            }

            Status = AppointmentStatus.Completed;
        }

        public void MarkNoShow()
        {
            if (Status is not AppointmentStatus.Pending and not AppointmentStatus.Confirmed)
            {
                throw new DomainException("Only pending or confirmed appointments can be marked as no-show.");
            }

            Status = AppointmentStatus.NoShow;
        }

        private static string FormatGracePeriod(int minutes)
        {
            if (minutes % 60 == 0)
            {
                var hours = minutes / 60;
                return hours == 1 ? "1 hour" : $"{hours} hours";
            }

            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }
    }
}
