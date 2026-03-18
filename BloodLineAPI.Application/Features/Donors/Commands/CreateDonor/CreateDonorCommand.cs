using BloodLineAPI.Domain.Enums;
using MediatR;

namespace BloodLineAPI.Application.Features.Donors.Commands.CreateDonor;

public sealed record CreateDonorCommand(
    Guid UserId,
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber) : IRequest<Guid>;
