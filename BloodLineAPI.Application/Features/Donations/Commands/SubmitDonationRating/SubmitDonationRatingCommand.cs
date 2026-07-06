using System;
using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Donations.Commands.SubmitDonationRating;

public sealed record SubmitDonationRatingCommand(
    Guid DonationId,
    Guid UserId,
    int StarScore,
    string? FeedbackText) : IRequest<Result<Unit>>;
