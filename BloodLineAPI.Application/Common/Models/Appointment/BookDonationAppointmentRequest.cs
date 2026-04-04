using BloodLineAPI.Application.Common.Models.PrescreeningEligibility;
using BloodLineAPI.Domain.Entities.DonationEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Common.Models.Appointment
{
    public sealed record BookDonationAppointmentRequest(
     DateTime ScheduledDate,
     TimeSpan BookTime,
     PrescreeningAnswers PrescreeningAnswers,
     Guid DonationCenterId,
     string DonationType);
}
