using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetSystemAppointmentById;

public sealed class GetSystemAppointmentByIdQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetSystemAppointmentByIdQuery, Result<SystemAppointmentDetailsDto>>
{
    public async Task<Result<SystemAppointmentDetailsDto>> Handle(GetSystemAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .Include(a => a.Donor)
                .ThenInclude(d => d.BloodType)
            .Include(a => a.DonationCenter)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (appointment == null)
        {
            return Result<SystemAppointmentDetailsDto>.Failure("Appointment not found.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - appointment.Donor.DateOfBirth.Year;
        if (appointment.Donor.DateOfBirth > today.AddYears(-age)) age--;

        var statusText = appointment.Status switch
        {
            AppointmentStatus.Pending => "booked",
            AppointmentStatus.Confirmed => "booked",
            AppointmentStatus.Completed => "completed",
            AppointmentStatus.Cancelled => "cancelled",
            AppointmentStatus.NoShow => "noshow",
            _ => "booked"
        };

        var dto = new SystemAppointmentDetailsDto(
            Id: appointment.Id,
            Date: appointment.ScheduledDate.ToString("yyyy-MM-dd"),
            Time: appointment.StartTime.ToString(@"hh\:mm"),
            Status: statusText,
            DonorName: appointment.Donor.FullName,
            DonorCode: appointment.Donor.DonorCode,
            DonorNationalId: appointment.Donor.NationalId,
            DonorPhone: appointment.Donor.PhoneNumber,
            DonorBloodType: appointment.Donor.BloodType?.FullDisplayname,
            DonorGender: appointment.Donor.Gender.ToString(),
            DonorAge: age,
            DonorDateOfBirth: appointment.Donor.DateOfBirth.ToString("yyyy-MM-dd"),
            DonorGovernorate: appointment.Donor.Governorate,
            DonorDistrict: appointment.Donor.District,
            DonorArea: appointment.Donor.Area,
            DonationType: appointment.DonationType.ToString().ToLowerInvariant(),
            CenterId: appointment.DonationCenterId,
            CenterType: appointment.DonationCenter.CenterType.ToString().ToLowerInvariant(),
            Notes: appointment.CancellationReason,
            CompletedAt: appointment.Status == AppointmentStatus.Completed ? appointment.EndTime.ToString(@"hh\:mm") : null,
            CancelledAt: appointment.CancelledAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CancellationReason: appointment.CancellationReason
        );

        return Result<SystemAppointmentDetailsDto>.Success(dto);
    }
}
