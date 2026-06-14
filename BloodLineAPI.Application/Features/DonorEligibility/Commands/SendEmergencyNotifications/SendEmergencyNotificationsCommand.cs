using System;
using System.Collections.Generic;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Commands.SendEmergencyNotifications;

public record SendEmergencyNotificationsCommand(
    List<Guid> DonorIds,
    string Type,
    string Message) : IRequest<Result<SendBulkNotificationResultDto>>;
