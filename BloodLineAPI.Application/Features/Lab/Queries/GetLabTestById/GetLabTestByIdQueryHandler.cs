using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetLabTestById;

public sealed class GetLabTestByIdQueryHandler : IRequestHandler<GetLabTestByIdQuery, GetLabTestByIdResult?>
{
    private readonly IApplicationDbContext _dbContext;

    public GetLabTestByIdQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<GetLabTestByIdResult?> Handle(GetLabTestByIdQuery request, CancellationToken cancellationToken)
    {
        var d = await _dbContext.DonationAppointments
            .AsNoTracking()
            .Include(x => x.Donor)
                .ThenInclude(donor => donor.BloodType)
            .Include(x => x.DonationCenter)
            .Include(x => x.BloodBag)
                .ThenInclude(bb => bb!.BloodTestResults)
                    .ThenInclude(r => r.TestedByStaff)
            .Include(x => x.BloodBag)
                .ThenInclude(bb => bb!.BloodTestResults)
                    .ThenInclude(r => r.ConfirmedBloodType)
            .FirstOrDefaultAsync(x => x.Id == request.DonationAppointmentId, cancellationToken);

        if (d?.BloodBag == null)
            return null;

        var latest = d.BloodBag.BloodTestResults.OrderByDescending(r => r.TestDate).FirstOrDefault();

        LabTestResultDto? result = null;
        if (latest != null)
        {
            var confirmedBt = latest.ConfirmedBloodType != null
                ? latest.ConfirmedBloodType.BloodGroupName.ToString() +
                  (latest.ConfirmedBloodType.RhFactor == Domain.Enums.RhFactor.Positive ? "+" : "-")
                : string.Empty;

            result = new LabTestResultDto(
                latest.IsSafe ? "safe" : "rejected",
                confirmedBt,
                latest.HepatitisCResult ?? string.Empty,
                latest.HepatitisBResult ?? string.Empty,
                latest.SyphilisResult ?? string.Empty,
                latest.HivResult ?? string.Empty,
                latest.Notes,
                latest.TestDate,
                latest.TestedByStaffId,
                latest.TestedByStaff?.FullName ?? string.Empty
            );
        }

        var donorBloodType = d.Donor.BloodType != null
            ? d.Donor.BloodType.BloodGroupName.ToString() +
              (d.Donor.BloodType.RhFactor == Domain.Enums.RhFactor.Positive ? "+" : "-")
            : string.Empty;

        return new GetLabTestByIdResult(
            d.Id,
            d.DonorId,
            d.Donor.FullName,
            d.BloodBag.SerialNumber,
            donorBloodType,
            d.DonationType.ToString().ToLowerInvariant(),
            d.DonationCenter.Location,
            d.ScheduledDate,
            d.BloodBag.BloodTestResults.Any() ? "completed" : "pending",
            result
        );
    }
}