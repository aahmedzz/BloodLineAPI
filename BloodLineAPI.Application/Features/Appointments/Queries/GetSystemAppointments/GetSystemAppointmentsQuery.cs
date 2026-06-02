using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetSystemAppointments;

public sealed record GetSystemAppointmentsQuery(
    Guid? CenterId,
    DateTime? Date,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? Status,
    Guid? CampaignId,
    int Page = 1,
    int Limit = 100
) : IRequest<Result<PaginatedAppointmentsResult>>;
