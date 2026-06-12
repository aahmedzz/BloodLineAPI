using System.Collections.Generic;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.UpdateCampaign;

public record UpdateCampaignCommand(
    string Id, // CampaignCode, e.g. "CAM-001"
    string? Title = null,
    string? City = null,
    double? Latitude = null,
    double? Longitude = null,
    string? StartTime = null,
    string? EndTime = null,
    int? SlotDuration = null,
    int? SlotCapacity = null,
    int? TargetDonors = null,
    string? Description = null,
    RecurrenceSettingsDto? Recurrence = null,
    List<string>? AvailableDonationTypes = null
) : IRequest<Result<CampaignDto>>;
