using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.ExportFailedDonorsPdf;

public sealed class ExportFailedDonorsPdfQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPdfGenerator pdfGenerator,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ExportFailedDonorsPdfQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportFailedDonorsPdfQuery request, CancellationToken cancellationToken)
    {
        // 1. Resolve performing staff name
        string staffName = "System";
        if (!string.IsNullOrEmpty(currentUserService.UserId) && Guid.TryParse(currentUserService.UserId, out var staffId))
        {
            var staff = await dbContext.Staff.FindAsync(new object[] { staffId }, cancellationToken);
            if (staff != null)
            {
                staffName = staff.FullName;
            }
        }

        // 2. Query failed notifications matching the target appeal ID
        var appealIdStr = request.AppealId.ToString();

        var failedNotifications = await dbContext.Notifications
            .AsNoTracking()
            .Include(n => n.User)
                .ThenInclude(u => u.Donor)
                    .ThenInclude(d => d!.BloodType)
            .Where(n => 
                n.Type == NotificationType.UrgentBloodAppeal &&
                n.IsSent == false &&
                n.ActionPayload != null &&
                n.ActionPayload.Contains(appealIdStr) &&
                n.User != null &&
                n.User.Donor != null)
            .ToListAsync(cancellationToken);

        var failedDonors = failedNotifications.Select(n => {
            var donor = n.User.Donor!;
            var reason = !string.IsNullOrEmpty(n.SentVia) 
                ? n.SentVia 
                : "فشل نظام الإشعارات في إرسال إشعار الهاتف";
            return new FailedDonorDto(
                donor.Id,
                donor.FullName,
                donor.PhoneNumber,
                donor.BloodType != null ? donor.BloodType.FullDisplayname : "غير معروف",
                reason
            );
        }).ToList();

        // 3. Generate report PDF
        var localNow = dateTimeProvider.LocalNow;
        return pdfGenerator.GenerateFailedDonorsReport(failedDonors, staffName, localNow);
    }
}
