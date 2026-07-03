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

        var failedDonors = await dbContext.Donors
            .AsNoTracking()
            .Include(d => d.BloodType)
            .Where(d => dbContext.Notifications.Any(n => 
                n.UserId == d.User.Id &&
                n.Type == NotificationType.UrgentBloodAppeal &&
                n.IsSent == false &&
                n.ActionPayload != null &&
                n.ActionPayload.Contains(appealIdStr)))
            .Select(d => new FailedDonorDto(
                d.Id,
                d.FullName,
                d.PhoneNumber,
                d.BloodType != null ? d.BloodType.FullDisplayname : "غير معروف",
                "فشل نظام الإشعارات في إرسال إشعار الهاتف"
            ))
            .ToListAsync(cancellationToken);

        // 3. Generate report PDF
        var localNow = dateTimeProvider.LocalNow;
        return pdfGenerator.GenerateFailedDonorsReport(failedDonors, staffName, localNow);
    }
}
