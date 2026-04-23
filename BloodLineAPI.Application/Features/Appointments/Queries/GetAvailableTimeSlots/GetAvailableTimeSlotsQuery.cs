using MediatR;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAvailableTimeSlots;

public sealed record GetAvailableTimeSlotsQuery(Guid DonationCenterId, DateTime Date) : IRequest<IReadOnlyList<TimeSlotDto>>;
