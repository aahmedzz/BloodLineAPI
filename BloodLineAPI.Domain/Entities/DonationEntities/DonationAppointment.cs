using BloodLineAPI.Domain.Events;

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
        public DonationType DonationType { get; set; }
        public AppointmentStatus Status { get; private set; } = AppointmentStatus.Pending;
        public DonationSource Source { get; private set; } = DonationSource.WalkIn;
        public string? CancellationReason { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public byte[] RowVersion { get; private set; } = [];

        // New properties for system donation flow
        public int DonationNumber { get; private set; }
        public string DonationCode { get; private set; } = string.Empty;
        public DonationStatus DonationStatus { get; private set; } = DonationStatus.Pending;
        public bool SentToLab { get; private set; } = false;
        public Guid? MedicalScreeningId { get; private set; }
        public TimeSpan? CheckInTime { get; private set; }

        public Donor Donor { get; set; } = null!;
        public DonationCenter DonationCenter { get; set; } = null!;
        public HealthPreScreening? HealthPreScreening { get; set; }
        public BloodBag? BloodBag { get; set; }
        public DonationRating? DonationRating { get; set; }
        public MedicalScreening? MedicalScreening { get; set; }

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
            TimeSpan openTime,
            TimeSpan closeTime,
            DateTime? lastDonationDate,
            DateTime? activeLockoutUntil,
            Gender donorGender,
            DonationCooldownSettings cooldownSettings,
            DateTime currentLocalTime,
            DonationSource source = DonationSource.WalkIn)
        {
            if (scheduledDate.Date < currentLocalTime.Date)
            {
                throw new DomainException("Cannot book an appointment in the past.");
            }

            if (scheduledDate.Date == currentLocalTime.Date && HasSlotPassed(startTime, currentLocalTime.TimeOfDay, openTime, closeTime))
            {
                throw new DomainException("Cannot book a time slot that has already passed.");
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

            if (activeLockoutUntil.HasValue && currentLocalTime < activeLockoutUntil.Value)
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
                EndTime = TimeSpan.FromTicks(startTime.Add(TimeSpan.FromMinutes(slotDurationMinutes)).Ticks % TimeSpan.TicksPerDay),
                DonationType = donationType,
                Status = AppointmentStatus.Pending,
                Source = source
            };
        }

        private static bool HasSlotPassed(TimeSpan startTime, TimeSpan currentTime, TimeSpan openTime, TimeSpan closeTime)
        {
            if (openTime <= closeTime)
            {
                return currentTime > startTime;
            }
            else
            {
                if (startTime >= openTime)
                {
                    return currentTime >= openTime && currentTime > startTime;
                }
                else
                {
                    return currentTime < openTime && currentTime > startTime;
                }
            }
        }

        public void Cancel(string reason, DateTime currentLocalTime, int gracePeriodMinutes = 30)
        {
            if (Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
            {
                throw new DomainException("Cannot cancel an appointment that is already cancelled or completed.");
            }

            var appointmentStart = ScheduledDate.Date.Add(StartTime);
            if (appointmentStart <= currentLocalTime)
            {
                throw new DomainException("Cannot cancel an appointment that has already started or passed.");
            }

            if ((appointmentStart - currentLocalTime).TotalMinutes < gracePeriodMinutes)
            {
                throw new DomainException($"Cannot cancel within {FormatGracePeriod(gracePeriodMinutes)} of the appointment. Please contact the center directly.");
            }

            Status = AppointmentStatus.Cancelled;
            CancellationReason = reason;
            CancelledAt = currentLocalTime;
        }

        public void CancelDueToIneligiblePreScreening(DateTime currentLocalTime)
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
            CancelledAt = currentLocalTime;
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
            TimeSpan openTime,
            TimeSpan closeTime,
            DateTime currentLocalTime,
            int gracePeriodMinutes = 30)
        {
            if (Status is not AppointmentStatus.Pending and not AppointmentStatus.Confirmed)
            {
                throw new DomainException("Only pending or confirmed appointments can be rescheduled.");
            }

            var currentStart = ScheduledDate.Date.Add(StartTime);
            if (currentStart <= currentLocalTime)
            {
                throw new DomainException("Cannot reschedule a past appointment.");
            }

            if ((currentStart - currentLocalTime).TotalMinutes < gracePeriodMinutes)
            {
                throw new DomainException($"Cannot reschedule within {FormatGracePeriod(gracePeriodMinutes)} of the appointment. Please contact the center directly.");
            }

            if (newDate.Date < currentLocalTime.Date)
            {
                throw new DomainException("Cannot reschedule to a past date.");
            }

            if (newDate.Date == currentLocalTime.Date && HasSlotPassed(newStartTime, currentLocalTime.TimeOfDay, openTime, closeTime))
            {
                throw new DomainException("Cannot reschedule to a time slot that has already passed.");
            }

            if (existingBookingsInSlot >= maxDonorsPerSlot)
            {
                throw new DomainException("This time slot is fully booked.");
            }

            ScheduledDate = newDate.Date;
            StartTime = newStartTime;
            EndTime = TimeSpan.FromTicks(newStartTime.Add(TimeSpan.FromMinutes(slotDurationMinutes)).Ticks % TimeSpan.TicksPerDay);
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

        public void MarkNoShow(DateTime currentLocalTime)
        {
            if (Status is not AppointmentStatus.Pending and not AppointmentStatus.Confirmed)
            {
                throw new DomainException("Only pending or confirmed appointments can be marked as no-show.");
            }

            var appointmentEnd = ScheduledDate.Date.Add(EndTime);
            if (appointmentEnd > currentLocalTime)
            {
                throw new DomainException("Cannot mark an appointment as no-show before its scheduled time has passed.");
            }

            Status = AppointmentStatus.NoShow;
        }

        public static DonationAppointment RegisterSystemDonation(
            Guid donorId,
            Guid donationCenterId,
            DonationType donationType,
            DonationSource source,
            bool isNewDonor,
            bool hasAppAccount,
            TimeSpan slotStart,
            TimeSpan slotEnd,
            DateTime currentLocalTime)
        {
            var appointment = new DonationAppointment
            {
                Id = Guid.NewGuid(),
                DonorId = donorId,
                DonationCenterId = donationCenterId,
                ScheduledDate = currentLocalTime.Date,
                StartTime = slotStart,
                EndTime = slotEnd,
                CheckInTime = currentLocalTime.TimeOfDay,
                DonationType = donationType,
                Status = AppointmentStatus.Confirmed,
                Source = source,
                DonationStatus = DonationStatus.Pending,
                SentToLab = false
            };

            appointment.AddDomainEvent(new DonationRegisteredEvent(
                donorId,
                appointment.Id,
                source,
                donationType,
                isNewDonor,
                hasAppAccount,
                currentLocalTime));

            return appointment;
        }

        public void UpdateSystemDonation(Guid donationCenterId, DonationSource source, TimeSpan slotStart, TimeSpan slotEnd, DateTime currentLocalTime)
        {
            if (DonationStatus != DonationStatus.Pending)
            {
                throw new DomainException("Only pending donations can be updated.");
            }

            DonationCenterId = donationCenterId;
            
            if (Source != DonationSource.MobileApp)
            {
                Source = source;
                ScheduledDate = currentLocalTime.Date;
                StartTime = slotStart;
                EndTime = slotEnd;
            }
            else
            {
                // If it is a mobile app booking for today, keep original slot. Otherwise, move to today's active slot.
                if (ScheduledDate.Date != currentLocalTime.Date)
                {
                    ScheduledDate = currentLocalTime.Date;
                    StartTime = slotStart;
                    EndTime = slotEnd;
                }
            }
            
            CheckInTime = currentLocalTime.TimeOfDay;
            Status = AppointmentStatus.Confirmed;
        }

        public void AttachMedicalScreening(Guid screeningId, DateTime currentLocalTime)
        {
            if (DonationStatus != DonationStatus.Pending)
            {
                throw new DomainException("Medical screening can only be attached to pending donations.");
            }

            MedicalScreeningId = screeningId;
            DonationStatus = DonationStatus.Approved;

            AddDomainEvent(new DonationScreeningCompletedEvent(
                DonorId,
                Id,
                screeningId,
                true,
                currentLocalTime));
        }

        public void RejectAfterScreening(Guid screeningId, DateTime currentLocalTime, string? rejectionReason = null)
        {
            if (DonationStatus != DonationStatus.Pending)
            {
                throw new DomainException("Medical screening can only be attached to pending donations.");
            }

            MedicalScreeningId = screeningId;
            DonationStatus = DonationStatus.Rejected;

            Status = AppointmentStatus.Cancelled;
            CancellationReason = rejectionReason ?? "Cancelled automatically due to ineligible medical screening.";
            CancelledAt = currentLocalTime;

            AddDomainEvent(new DonationScreeningCompletedEvent(
                DonorId,
                Id,
                screeningId,
                false,
                currentLocalTime));
        }

        public void SendToLab(Guid bloodBagId, DateTime currentLocalTime)
        {
            if (DonationStatus != DonationStatus.Approved)
            {
                throw new DomainException("Donation must be approved to send to lab.");
            }

            SentToLab = true;
            DonationStatus = DonationStatus.Completed;
            MarkCompleted();

            AddDomainEvent(new DonationSentToLabEvent(
                DonorId,
                Id,
                bloodBagId,
                currentLocalTime));
        }

        public void CancelDonation(DateTime currentLocalTime)
        {
            if (DonationStatus is DonationStatus.Completed or DonationStatus.Cancelled or DonationStatus.Rejected)
            {
                throw new DomainException("Cannot cancel a completed, rejected, or already cancelled donation.");
            }

            DonationStatus = DonationStatus.Cancelled;
            Status = AppointmentStatus.Cancelled;
            CancellationReason = "Problem in collecting the blood donation.";
            CancelledAt = currentLocalTime;
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
