using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemands
{
    public sealed class GetBloodDemandsQueryHandler : IRequestHandler<GetBloodDemandsQuery, GetBloodDemandsResult>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetBloodDemandsQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GetBloodDemandsResult> Handle(GetBloodDemandsQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.BloodDemands
                .AsNoTracking()
                .Include(bd => bd.BloodType)
                .AsQueryable();

            // Search filter: matches RequesterName
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(bd => bd.RequesterName.Contains(search));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<BloodDemandStatus>(request.Status, ignoreCase: true, out var statusEnum))
                {
                    query = query.Where(bd => bd.Status == statusEnum);
                }
            }

            // Blood type filter
            if (!string.IsNullOrWhiteSpace(request.BloodType))
            {
                var bt = request.BloodType.Trim();
                query = query.Where(bd =>
                    bd.BloodType != null &&
                    (bd.BloodType.BloodGroupName.ToString() +
                     (bd.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt);
            }

            // Priority filter
            if (!string.IsNullOrWhiteSpace(request.Priority))
            {
                if (Enum.TryParse<BloodDemandPriority>(request.Priority, ignoreCase: true, out var priorityEnum))
                {
                    query = query.Where(bd => bd.Priority == priorityEnum);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // Sorting: default to newest request date first
            query = query.OrderByDescending(bd => bd.RequestDate);

            // Pagination
            var page = Math.Max(1, request.Page);
            var limit = Math.Clamp(request.Limit, 1, 100);
            var totalPages = (int)Math.Ceiling((double)totalCount / limit);

            var rawItems = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);

            var items = rawItems.Select(bd => new BloodDemandDto(
                bd.Id,
                bd.RequestDate,
                bd.BloodType != null ? bd.BloodType.BloodGroupName.ToString() + (bd.BloodType.RhFactor == RhFactor.Positive ? "+" : "-") : string.Empty,
                bd.RequesterName,
                bd.RequestedUnits,
                bd.IssuedUnits,
                bd.RemainingUnits,
                bd.Priority.ToString(),
                bd.Status.ToString(),
                bd.Notes,
                bd.CreatedAt
            )).ToList();

            return new GetBloodDemandsResult(items, page, limit, totalCount, totalPages);
        }
    }
}
