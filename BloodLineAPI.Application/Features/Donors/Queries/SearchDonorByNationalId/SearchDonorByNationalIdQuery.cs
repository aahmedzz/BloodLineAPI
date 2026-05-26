using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;
using MediatR;

namespace BloodLineAPI.Application.Features.Donors.Queries.SearchDonorByNationalId;

public record SearchDonorByNationalIdQuery(string NationalId) : IRequest<Result<FilteredDonorDto>>;
