using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using BloodLineAPI.Application.Features.DonorEligibility.Commands.SendEmergencyNotifications;
using BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEmergencyNotificationPreview;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IEmergencyNotificationService
{
    Task<Result<SendBulkNotificationResultDto>> SendBulkEmergencyNotificationAsync(
        SendEmergencyNotificationsCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationPreviewResponseDto>> GetEmergencyNotificationPreviewAsync(
        GetEmergencyNotificationPreviewQuery query,
        CancellationToken cancellationToken = default);
}
