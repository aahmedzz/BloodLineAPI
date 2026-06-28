using MediatR;
using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetDonorBadgeHistory;

public sealed record GetDonorBadgeHistoryQuery(Guid DonorId) : IRequest<IReadOnlyList<BadgeHistoryItemDto>>;
