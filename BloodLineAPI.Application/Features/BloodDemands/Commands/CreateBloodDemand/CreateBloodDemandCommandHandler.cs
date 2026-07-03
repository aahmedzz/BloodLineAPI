using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.BloodDemands.Commands.CreateBloodDemand
{
    public sealed class CreateBloodDemandCommandHandler : IRequestHandler<CreateBloodDemandCommand, Guid>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateBloodDemandCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Guid> Handle(CreateBloodDemandCommand request, CancellationToken cancellationToken)
        {
            // Verify that blood type exists
            var bloodTypeExists = await _dbContext.BloodTypes
                .AnyAsync(bt => bt.Id == request.BloodTypeId, cancellationToken);

            if (!bloodTypeExists)
            {
                throw new ArgumentException("فصيلة الدم المحددة غير موجودة.");
            }

            var demand = new BloodDemand
            {
                Id = Guid.NewGuid(),
                RequestDate = _dateTimeProvider.LocalNow,
                BloodTypeId = request.BloodTypeId,
                RequesterName = request.RequesterName,
                RequestedUnits = request.RequestedUnits,
                IssuedUnits = 0,
                Priority = request.Priority,
                Status = BloodDemandStatus.Pending,
                Notes = request.Notes,
                CreatedBy = _currentUserService.UserId
            };

            await _dbContext.BloodDemands.AddAsync(demand, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return demand.Id;
        }
    }
}
