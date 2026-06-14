using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IEmergencyNotificationService
{
    Task<Result<SendBulkNotificationResultDto>> SendBulkEmergencyNotificationAsync(
        List<Guid> donorIds,
        string message,
        CancellationToken cancellationToken = default);
}
