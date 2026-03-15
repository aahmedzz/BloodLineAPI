using BloodBankSystem.Domain.Enums;
using MediatR;

namespace BloodLineAPI.Application.Features.Donors.Commands.CreateDonor;

public sealed record CreateDonorCommand(
    string FullName,
    DateOnly DateOfBirth,
    BloodType BloodType,
    string PhoneNumber) : IRequest<Guid>;
