using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Donations.Commands.ConfirmDonation;

public record ConfirmDonationCommand(Guid DonationId) : IRequest<Result<string>>;
