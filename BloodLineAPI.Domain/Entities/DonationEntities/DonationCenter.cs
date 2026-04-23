namespace BloodLineAPI.Domain.Entities.DonationEntities
{
    public class DonationCenter : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AddressDetails { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public CenterType CenterType { get; set; }
        public CenterStatus Status { get; set; } = CenterStatus.Active;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? DescriptionText { get; set; }
        public int MaxDonorsPerSlot { get; set; }
        public int? SlotDurationMinutes { get; set; }

        public ICollection<DonationAppointment> DonationAppointments { get; set; } = new List<DonationAppointment>();
        public ICollection<OpeningHours> OpeningHours { get; set; } = new List<OpeningHours>();
        public ICollection<CenterExclusion> CenterExclusions { get; set; } = new List<CenterExclusion>();

        public (TimeSpan Open, TimeSpan Close, int MaxPerSlot)? ResolveOperatingHours(
            DateTime date,
            IReadOnlyList<CenterExclusion>? exclusions = null,
            IReadOnlyList<OpeningHours>? weeklyHours = null)
        {
            exclusions ??= CenterExclusions.ToList();
            weeklyHours ??= OpeningHours.ToList();

            var exclusion = exclusions.FirstOrDefault(e => e.Date.Date == date.Date);
            if (exclusion is not null)
            {
                if (exclusion.IsClosed)
                {
                    return null;
                }

                return (
                    exclusion.SpecialOpeningTime ?? StartTime,
                    exclusion.SpecialClosingTime ?? EndTime,
                    MaxDonorsPerSlot);
            }

            var daySchedule = weeklyHours.FirstOrDefault(h => h.DayOfWeek == date.DayOfWeek);
            if (daySchedule is not null)
            {
                if (daySchedule.IsClosed)
                {
                    return null;
                }

                return (
                    daySchedule.OpeningTime,
                    daySchedule.ClosingTime,
                    daySchedule.MaxDonorsPerSlot ?? MaxDonorsPerSlot);
            }

            return (StartTime, EndTime, MaxDonorsPerSlot);
        }

        public IReadOnlyList<(TimeSpan Start, TimeSpan End, int MaxPerSlot)> GenerateTimeSlotsForDate(
            DateTime date,
            IReadOnlyList<CenterExclusion>? exclusions = null,
            IReadOnlyList<OpeningHours>? weeklyHours = null)
        {
            var hours = ResolveOperatingHours(date, exclusions, weeklyHours);
            if (hours is null)
            {
                return [];
            }

            var (open, close, maxPerSlot) = hours.Value;
            var slotMinutes = SlotDurationMinutes ?? 15;
            var slots = new List<(TimeSpan Start, TimeSpan End, int MaxPerSlot)>();
            var current = open;

            while (current.Add(TimeSpan.FromMinutes(slotMinutes)) <= close)
            {
                var end = current.Add(TimeSpan.FromMinutes(slotMinutes));
                slots.Add((current, end, maxPerSlot));
                current = end;
            }

            return slots;
        }

        public bool IsOperatingOn(DateTime date)
        {
            return date.Date >= StartDate.Date && (EndDate == null || date.Date <= EndDate.Value.Date);
        }
    }
}
