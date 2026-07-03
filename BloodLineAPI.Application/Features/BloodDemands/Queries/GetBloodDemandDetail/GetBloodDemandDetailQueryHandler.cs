using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemandDetail
{
    public sealed class GetBloodDemandDetailQueryHandler : IRequestHandler<GetBloodDemandDetailQuery, BloodDemandDetailDto?>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetBloodDemandDetailQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<BloodDemandDetailDto?> Handle(GetBloodDemandDetailQuery request, CancellationToken cancellationToken)
        {
            var demand = await _dbContext.BloodDemands
                .AsNoTracking()
                .Include(bd => bd.BloodType)
                .Include(bd => bd.IssuanceRecords)
                    .ThenInclude(ir => ir.BloodBag)
                .Include(bd => bd.IssuanceRecords)
                    .ThenInclude(ir => ir.IssuedByStaff)
                .FirstOrDefaultAsync(bd => bd.Id == request.Id, cancellationToken);

            if (demand == null)
            {
                return null;
            }

            var bloodTypeStr = demand.BloodType != null 
                ? demand.BloodType.BloodGroupName.ToString() + (demand.BloodType.RhFactor == Domain.Enums.RhFactor.Positive ? "+" : "-") 
                : string.Empty;

            var history = demand.IssuanceRecords
                .OrderByDescending(ir => ir.IssuedAt)
                .Select(ir => new BloodDemandIssuanceHistoryDto(
                    ir.Id,
                    ir.IssuedAt,
                    ir.IssuedByStaff?.FullName ?? string.Empty,
                    ir.BloodBag?.SerialNumber ?? string.Empty,
                    ir.RecipientName,
                    ir.NationalId,
                    ir.Phone,
                    ir.Reason
                ))
                .ToList();

            return new BloodDemandDetailDto(
                demand.Id,
                demand.RequestDate,
                bloodTypeStr,
                demand.RequesterName,
                demand.RequestedUnits,
                demand.IssuedUnits,
                demand.RemainingUnits,
                demand.Priority.ToString(),
                demand.Status.ToString(),
                demand.Notes,
                demand.CreatedAt,
                history
            );
        }
    }
}
