using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.DonorEligibility.Commands.SendEmergencyNotifications;

public sealed class SendEmergencyNotificationsCommandHandler(
    IEmergencyNotificationService emergencyNotificationService)
    : IRequestHandler<SendEmergencyNotificationsCommand, Result<SendBulkNotificationResultDto>>
{
    public async Task<Result<SendBulkNotificationResultDto>> Handle(
        SendEmergencyNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        return await emergencyNotificationService.SendBulkEmergencyNotificationAsync(
            request,
            cancellationToken);
    }
}
