using BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;
using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateDonor;

public record UpdateDonorCommand(
    Guid Id,
    string? Name = null,
    string? Phone = null,
    string? BloodType = null,
    string? Governorate = null,
    string? District = null,
    string? Area = null,
    string? NationalId = null,
    string? DateOfBirth = null) : IRequest<Result<FilteredDonorDto>>;
