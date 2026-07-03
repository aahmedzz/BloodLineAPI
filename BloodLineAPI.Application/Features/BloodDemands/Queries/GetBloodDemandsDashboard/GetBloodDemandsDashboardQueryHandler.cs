using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemandsDashboard
{
    public sealed class GetBloodDemandsDashboardQueryHandler : IRequestHandler<GetBloodDemandsDashboardQuery, BloodDemandsDashboardResult>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetBloodDemandsDashboardQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<BloodDemandsDashboardResult> Handle(GetBloodDemandsDashboardQuery request, CancellationToken cancellationToken)
        {
            var total = await _dbContext.BloodDemands.CountAsync(cancellationToken);
            
            var pending = await _dbContext.BloodDemands.CountAsync(
                bd => bd.Status == BloodDemandStatus.Pending, 
                cancellationToken);

            var partiallyFulfilled = await _dbContext.BloodDemands.CountAsync(
                bd => bd.Status == BloodDemandStatus.PartiallyFulfilled, 
                cancellationToken);

            var fulfilled = await _dbContext.BloodDemands.CountAsync(
                bd => bd.Status == BloodDemandStatus.Fulfilled, 
                cancellationToken);

            return new BloodDemandsDashboardResult(total, pending, partiallyFulfilled, fulfilled);
        }
    }
}
