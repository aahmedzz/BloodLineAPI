using MediatR;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetAllDonors;

public sealed record GetAllDonorsQuery : IRequest<IReadOnlyList<DonorDto>>;
