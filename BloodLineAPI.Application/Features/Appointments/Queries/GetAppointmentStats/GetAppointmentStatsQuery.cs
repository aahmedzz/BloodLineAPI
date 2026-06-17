using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAppointmentStats;

public sealed record GetAppointmentStatsQuery(
    Guid? CenterId,
    DateTime? Date,
    DateTime? DateFrom,
    DateTime? DateTo
) : IRequest<Result<AppointmentStatsDto>>;
