using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetDonorById;

public record GetDonorByIdQuery(Guid Id) : IRequest<Result<FilteredDonorDto>>;
