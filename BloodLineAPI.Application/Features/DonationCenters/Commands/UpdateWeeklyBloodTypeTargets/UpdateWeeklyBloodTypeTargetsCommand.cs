using System;
using System.Collections.Generic;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonationCenters.Commands.UpdateWeeklyBloodTypeTargets
{
    public record UpdateWeeklyBloodTypeTargetsCommand(
        Guid CenterId,
        IReadOnlyList<UpdateWeeklyBloodTypeTargetDto> Targets)
        : IRequest<Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>>;
}
