namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAvailableTimeSlots;

public sealed record TimeSlotDto(
    string StartTime,
    string EndTime,
    int AvailableCapacity,
    bool IsAvailable);
