using BloodLineAPI.Application.Common.Models;
using MediatR;

using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateDonorLocation;

public sealed record UpdateDonorLocationCommand(
    [property: JsonIgnore] Guid UserId,
    double Latitude,
    double Longitude
) : IRequest<Result<string>>;
