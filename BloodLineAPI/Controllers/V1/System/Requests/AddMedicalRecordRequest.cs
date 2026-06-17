using BloodLineAPI.Application.Features.Donations.Commands.AddMedicalRecord;

namespace BloodLineAPI.Controllers.V1.System.Requests;

public sealed record AddMedicalRecordRequest(
    string Status,
    string[] Diseases,
    MedicalRecordAdditionalData AdditionalData,
    bool IsAllergic,
    string? RejectionReason,
    string? DeferredUntil,
    string DonationType,
    string? BloodType);
