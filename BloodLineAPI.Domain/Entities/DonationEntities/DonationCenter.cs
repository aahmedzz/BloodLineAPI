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

        // Campaign-specific properties
        public int CampaignNumber { get; set; }
        public string? CampaignCode { get; set; }
        public int? TargetDonors { get; set; }
        public Guid? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public bool RecurrenceEnabled { get; set; }
        public RecurrenceType? RecurrenceType { get; set; }
        public string? RecurrenceWeekDays { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
        public Guid? RecurrenceGroupId { get; set; }
        public string? ScheduledJobIds { get; set; }

        public void CompleteCampaignEarly()
        {
            if (CenterType != CenterType.Campaign)
                throw new DomainException("Only campaigns can be completed early.");
            if (Status == CenterStatus.Completed)
                throw new DomainException("Campaign is already completed.");
            Status = CenterStatus.Completed;
        }

        public ICollection<DonationAppointment> DonationAppointments { get; set; } = new List<DonationAppointment>();
        public ICollection<OpeningHours> OpeningHours { get; set; } = new List<OpeningHours>();
        public ICollection<CenterExclusion> CenterExclusions { get; set; } = new List<CenterExclusion>();
        public ICollection<BloodTypeTargets> BloodTypeTargets { get; set; } = new List<BloodTypeTargets>();

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
            var slots = new List<(TimeSpan Start, TimeSpan End, int MaxPerSlot)>();

            // 1. Generate slots starting on this date (if scheduled)
            if (IsScheduledForDate(date)) // date-only check: scheduled for today
            {
                var hours = ResolveOperatingHours(date, exclusions, weeklyHours);
                if (hours is not null)
                {
                    var (open, close, maxPerSlot) = hours.Value;
                    var slotMinutes = SlotDurationMinutes ?? 15;
                    var current = open;

                    if (open <= close)
                    {
                        // Standard non-overnight slot generation
                        while (current.Add(TimeSpan.FromMinutes(slotMinutes)) <= close)
                        {
                            var end = current.Add(TimeSpan.FromMinutes(slotMinutes));
                            slots.Add((current, end, maxPerSlot));
                            current = end;
                        }
                    }
                    else
                    {
                        // Overnight campaign: generate slots starting today up to midnight (24:00)
                        var midnight = TimeSpan.FromHours(24);
                        while (current.Add(TimeSpan.FromMinutes(slotMinutes)) <= midnight)
                        {
                            var end = current.Add(TimeSpan.FromMinutes(slotMinutes));
                            
                            var slotStart = TimeSpan.FromTicks(current.Ticks % TimeSpan.TicksPerDay);
                            var slotEnd = TimeSpan.FromTicks(end.Ticks % TimeSpan.TicksPerDay);
                            
                            slots.Add((slotStart, slotEnd, maxPerSlot));
                            current = end;
                        }
                    }
                }
            }

            // 2. Generate slots ending on this date from a session that started yesterday (if overnight)
            if (CenterType == CenterType.Campaign && StartTime > EndTime)
            {
                var yesterday = date.AddDays(-1);
                if (IsScheduledForDate(yesterday)) // date-only check: scheduled for yesterday
                {
                    var hours = ResolveOperatingHours(yesterday, exclusions, weeklyHours);
                    if (hours is not null)
                    {
                        var (_, close, maxPerSlot) = hours.Value;
                        var slotMinutes = SlotDurationMinutes ?? 15;
                        var current = TimeSpan.Zero; // starts at midnight

                        while (current.Add(TimeSpan.FromMinutes(slotMinutes)) <= close)
                        {
                            var end = current.Add(TimeSpan.FromMinutes(slotMinutes));
                            
                            var slotStart = TimeSpan.FromTicks(current.Ticks % TimeSpan.TicksPerDay);
                            var slotEnd = TimeSpan.FromTicks(end.Ticks % TimeSpan.TicksPerDay);
                            
                            slots.Add((slotStart, slotEnd, maxPerSlot));
                            current = end;
                        }
                    }
                }
            }

            return slots;
        }

        public bool IsOperatingOn(DateTime date)
        {
            if (CenterType != CenterType.Campaign)
            {
                return date.Date >= StartDate.Date && (EndDate == null || date.Date <= EndDate.Value.Date);
            }

            if (date.TimeOfDay == TimeSpan.Zero)
            {
                return IsScheduledForDate(date) || (StartTime > EndTime && IsScheduledForDate(date.AddDays(-1)));
            }

            if (IsScheduledForDate(date))
            {
                var time = date.TimeOfDay;
                if (StartTime <= EndTime)
                {
                    if (time >= StartTime && time <= EndTime)
                        return true;
                }
                else
                {
                    if (time >= StartTime)
                        return true;
                }
            }

            if (StartTime > EndTime)
            {
                var prevDate = date.AddDays(-1);
                if (IsScheduledForDate(prevDate))
                {
                    var time = date.TimeOfDay;
                    if (time <= EndTime)
                        return true;
                }
            }

            return false;
        }

        private bool IsScheduledForDate(DateTime date)
        {
            var targetDate = date.Date;
            if (targetDate < StartDate.Date) return false;
            if (EndDate.HasValue && targetDate > EndDate.Value.Date) return false;

            if (CenterType != CenterType.Campaign)
            {
                return true;
            }

            if (RecurrenceEndDate.HasValue && targetDate > RecurrenceEndDate.Value.Date) return false;

            if (!RecurrenceEnabled || RecurrenceType == BloodLineAPI.Domain.Enums.RecurrenceType.None)
            {
                return targetDate == StartDate.Date;
            }

            switch (RecurrenceType)
            {
                case BloodLineAPI.Domain.Enums.RecurrenceType.Daily:
                    return true;

                case BloodLineAPI.Domain.Enums.RecurrenceType.Weekly:
                    return targetDate.DayOfWeek == StartDate.DayOfWeek;

                case BloodLineAPI.Domain.Enums.RecurrenceType.Monthly:
                    return targetDate.Day == StartDate.Day;

                case BloodLineAPI.Domain.Enums.RecurrenceType.Custom:
                    if (string.IsNullOrEmpty(RecurrenceWeekDays)) return false;
                    var allowedDays = RecurrenceWeekDays.Split(',').Select(int.Parse).ToList();
                    return allowedDays.Contains((int)targetDate.DayOfWeek);

                default:
                    return false;
            }
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
