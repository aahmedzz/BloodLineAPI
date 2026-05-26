using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Donations.Commands.DeleteDonation;

public record DeleteDonationCommand(Guid DonationId) : IRequest<Result<string>>;
