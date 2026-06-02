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
        public string SupportedDonationTypes { get; set; } = string.Join(',', Enum.GetNames<DonationType>());
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

        public (TimeSpan Start, TimeSpan End) FindSlotForTime(DateTime dateTime)
        {
            var slots = GenerateTimeSlotsForDate(dateTime);
            var time = dateTime.TimeOfDay;

            if (slots.Count == 0)
            {
                // Fallback: round to nearest SlotDurationMinutes
                var duration = SlotDurationMinutes ?? 15;
                var minutes = time.Minutes;
                var roundedMinutes = (minutes / duration) * duration;
                var start = new TimeSpan(time.Hours, roundedMinutes, 0);
                var end = start.Add(TimeSpan.FromMinutes(duration));
                return (start, end);
            }

            var matchingSlot = slots.FirstOrDefault(s => time >= s.Start && time < s.End);
            if (matchingSlot != default)
            {
                return (matchingSlot.Start, matchingSlot.End);
            }

            // Fallback: if before opening hours, return first slot
            if (time < slots.First().Start)
            {
                return (slots.First().Start, slots.First().End);
            }

            // Fallback: if after closing hours, return last slot
            return (slots.Last().Start, slots.Last().End);
        }

        public IReadOnlyList<string> GetSupportedDonationTypes()
        {
            return SupportedDonationTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public bool SupportsDonationType(DonationType donationType)
        {
            return GetSupportedDonationTypes().Contains(donationType.ToString(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
