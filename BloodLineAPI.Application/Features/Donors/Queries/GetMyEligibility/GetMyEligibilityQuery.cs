using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetMyEligibility;

public sealed record GetMyEligibilityQuery(Guid UserId)
    : IRequest<Result<MyEligibilityResponse>>;
