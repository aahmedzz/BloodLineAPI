using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Queries.GetCurrentMobileUser;

public sealed record GetCurrentMobileUserQuery(Guid UserId)
    : IRequest<Result<MobileUserProfileResponse>>;
