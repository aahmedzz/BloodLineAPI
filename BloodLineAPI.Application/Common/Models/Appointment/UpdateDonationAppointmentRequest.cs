using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Common.Models.Appointment
{
    public sealed record UpdateDonationAppointmentRequest(
    DateTime ScheduledDate,
    TimeSpan BookTime,
    string DonationType);
}
