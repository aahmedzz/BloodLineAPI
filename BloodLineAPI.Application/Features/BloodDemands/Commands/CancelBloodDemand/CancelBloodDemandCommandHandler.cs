using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.BloodDemands.Commands.CancelBloodDemand
{
    public sealed class CancelBloodDemandCommandHandler : IRequestHandler<CancelBloodDemandCommand>
    {
        private readonly IApplicationDbContext _dbContext;

        public CancelBloodDemandCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(CancelBloodDemandCommand request, CancellationToken cancellationToken)
        {
            var demand = await _dbContext.BloodDemands
                .FirstOrDefaultAsync(bd => bd.Id == request.Id, cancellationToken);

            if (demand == null)
            {
                throw new NotFoundException("BloodDemand", request.Id);
            }

            if (demand.Status == BloodDemandStatus.Fulfilled || demand.Status == BloodDemandStatus.Cancelled)
            {
                throw new DomainException("الطلب مكتمل أو ملغي بالفعل ولا يمكن تعديله.");
            }

            if (demand.Status == BloodDemandStatus.Pending || demand.Status == BloodDemandStatus.Approved)
            {
                demand.Status = BloodDemandStatus.Cancelled;
            }
            else if (demand.Status == BloodDemandStatus.PartiallyFulfilled)
            {
                demand.Status = BloodDemandStatus.Fulfilled;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
