using BloodLineAPI.Application.Features.Appointments.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonationCenters;

public sealed record GetDonationCentersQuery(string? SearchTerm) : IRequest<IReadOnlyList<DonationCenterDto>>;
