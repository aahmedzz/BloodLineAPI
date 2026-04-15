namespace BloodLineAPI.Application.Features.Gamification.Models;

public sealed record GamificationContext(
    Guid DonorId,
    GamificationTrigger Trigger,
    DateTime OccurredOn,
    Guid? DonationAppointmentId = null,
    bool IsEmergencyDonation = false,
    Guid? UrgentBloodAppealId = null,
    Guid? MedicalInfoId = null);
