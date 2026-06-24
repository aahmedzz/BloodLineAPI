using BloodLineAPI.Application.Common.Models;
using MediatR;

using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateMobileProfile;

public sealed record UpdateMobileProfileCommand(
    [property: JsonIgnore] Guid UserId,            // injected from JWT claim — never from request body
    string?  DateOfBirth,       // "yyyy-MM-dd", optional
    string?  PhoneNumber,       // optional
    decimal? WeightKg,          // optional
    string?  Governorate,       // optional
    string?  District,          // optional
    string?  Area              // optional
) : IRequest<Result<MobileUserProfileResponse>>;
