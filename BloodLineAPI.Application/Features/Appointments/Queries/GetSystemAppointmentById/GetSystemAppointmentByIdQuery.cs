using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetSystemAppointmentById;

public sealed record GetSystemAppointmentByIdQuery(Guid Id) : IRequest<Result<SystemAppointmentDetailsDto>>;
