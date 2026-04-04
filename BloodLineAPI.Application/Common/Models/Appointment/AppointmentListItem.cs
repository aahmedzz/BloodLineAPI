using BloodLineAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Common.Models.MobileAppointment
{ 
    public sealed record AppointmentListItem(
    Guid Id,
    string DonationType,
    DateTime ScheduledDate,
    TimeSpan BookTime,
    AppointmentStatus Status,
    Guid DonationCenterId,
    string CenterName,
    string CenterLocation);
}
