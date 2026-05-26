using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Donations.Commands.AddMedicalRecord;

public record MedicalRecordAdditionalData(
    decimal Weight,
    string BloodPressure,
    decimal Hemoglobin);

public record AddMedicalRecordCommand(
    Guid DonationId,
    string Status,
    string[] Diseases,
    MedicalRecordAdditionalData AdditionalData,
    bool IsAllergic,
    string? RejectionReason,
    string? DeferredUntil,
    string DonationType,
    string? BloodType) : IRequest<Result<string>>;
