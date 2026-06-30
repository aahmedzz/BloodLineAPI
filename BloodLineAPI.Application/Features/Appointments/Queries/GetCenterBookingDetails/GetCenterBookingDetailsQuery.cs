using BloodLineAPI.Application.Features.Appointments.Dtos;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetCenterBookingDetails;

public sealed record GetCenterBookingDetailsQuery(Guid CenterId, Guid DonorId) : IRequest<BookingDetailsDto?>;
