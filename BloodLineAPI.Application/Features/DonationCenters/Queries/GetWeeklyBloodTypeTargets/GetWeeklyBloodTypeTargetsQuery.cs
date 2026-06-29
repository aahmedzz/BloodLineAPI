using System;
using System.Collections.Generic;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonationCenters.Queries.GetWeeklyBloodTypeTargets
{
    public record GetWeeklyBloodTypeTargetsQuery(Guid CenterId) 
        : IRequest<Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>>;
}
