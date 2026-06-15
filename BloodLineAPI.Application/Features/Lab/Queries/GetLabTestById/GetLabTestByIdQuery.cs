using MediatR;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetLabTestById;

public sealed record GetLabTestByIdQuery(Guid DonationAppointmentId) : IRequest<GetLabTestByIdResult?>;