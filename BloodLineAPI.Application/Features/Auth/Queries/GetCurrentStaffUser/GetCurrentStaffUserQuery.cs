using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.Auth;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Queries.GetCurrentStaffUser;

public sealed record GetCurrentStaffUserQuery(Guid UserId) : IRequest<Result<AuthenticatedStaffUser>>;
