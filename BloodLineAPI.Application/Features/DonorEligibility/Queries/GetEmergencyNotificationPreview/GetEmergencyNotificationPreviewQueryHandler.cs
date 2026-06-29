using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEmergencyNotificationPreview;

public sealed class GetEmergencyNotificationPreviewQueryHandler(
    IEmergencyNotificationService emergencyNotificationService)
    : IRequestHandler<GetEmergencyNotificationPreviewQuery, Result<NotificationPreviewResponseDto>>
{
    public async Task<Result<NotificationPreviewResponseDto>> Handle(
        GetEmergencyNotificationPreviewQuery request,
        CancellationToken cancellationToken)
    {
        return await emergencyNotificationService.GetEmergencyNotificationPreviewAsync(
            request,
            cancellationToken);
    }
}
