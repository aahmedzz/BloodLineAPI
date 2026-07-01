using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetMobileDonationHistory;

public sealed record GetMobileDonationHistoryQuery(Guid DonorId, string? DonationType) : IRequest<Result<IReadOnlyList<DonationHistoryItemDto>>>;
