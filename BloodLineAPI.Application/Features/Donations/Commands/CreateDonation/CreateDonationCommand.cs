using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Donations.Commands.CreateDonation;

public record CreateDonationCommand(
    string NationalId,
    string Name,
    string Gender,
    string DateOfBirth,
    string Phone,
    string Governorate,
    string District,
    string? Area,
    string Source,
    Guid DonationCenterId) : IRequest<Result<Guid>>;
