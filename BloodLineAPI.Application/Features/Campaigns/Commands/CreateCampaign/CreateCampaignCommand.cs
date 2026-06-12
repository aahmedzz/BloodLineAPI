using System.Collections.Generic;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.CreateCampaign;

public record CreateCampaignCommand(
    string Title,
    string City,
    double? Latitude,
    double? Longitude,
    string StartTime,
    string EndTime,
    int SlotDuration,
    int SlotCapacity,
    int TargetDonors,
    string Description,
    RecurrenceSettingsDto? Recurrence,
    List<string> AvailableDonationTypes
) : IRequest<Result<CampaignDto>>;
